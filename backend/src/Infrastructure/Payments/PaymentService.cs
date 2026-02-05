using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Enums;
using TelegramMarketplace.Domain.Interfaces;
using TelegramMarketplace.Infrastructure.Configuration;
using TelegramMarketplace.Infrastructure.Payments.Robokassa;
using TelegramMarketplace.Infrastructure.Payments.TelegramStars;
using TelegramMarketplace.Infrastructure.Payments.YooKassa;

namespace TelegramMarketplace.Infrastructure.Payments;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IYooKassaClient _yooKassaClient;
    private readonly IRobokassaClient _robokassaClient;
    private readonly ITelegramStarsClient _telegramStarsClient;
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;
    private readonly YooKassaSettings _yooKassaSettings;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IYooKassaClient yooKassaClient,
        IRobokassaClient robokassaClient,
        ITelegramStarsClient telegramStarsClient,
        IOrderService orderService,
        INotificationService notificationService,
        IOptions<YooKassaSettings> yooKassaSettings,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _yooKassaClient = yooKassaClient;
        _robokassaClient = robokassaClient;
        _telegramStarsClient = telegramStarsClient;
        _orderService = orderService;
        _notificationService = notificationService;
        _yooKassaSettings = yooKassaSettings.Value;
        _logger = logger;
    }

    public async Task<Result<PaymentResultDto>> CreatePaymentAsync(
        Guid orderId,
        PaymentProvider provider,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId, cancellationToken);
        if (order == null)
            return Result.Failure<PaymentResultDto>("Order not found", "NOT_FOUND");

        if (order.PaymentStatus != PaymentStatus.Pending)
            return Result.Failure<PaymentResultDto>("Order already has a payment", "ALREADY_PAID");

        var payment = Payment.Create(orderId, order.TotalAmount, provider);
        await _unitOfWork.Payments.AddAsync(payment, cancellationToken);

        string? confirmationUrl = null;

        switch (provider)
        {
            case PaymentProvider.YooKassa:
                var yooKassaResult = await CreateYooKassaPaymentAsync(payment, order, returnUrl ?? _yooKassaSettings.ReturnUrl, cancellationToken);
                if (yooKassaResult == null)
                    return Result.Failure<PaymentResultDto>("Failed to create YooKassa payment", "PAYMENT_ERROR");
                confirmationUrl = yooKassaResult.Confirmation?.ConfirmationUrl;
                payment.SetExternalId(yooKassaResult.Id);
                payment.SetConfirmationUrl(confirmationUrl ?? "");
                break;

            case PaymentProvider.Robokassa:
                var robokassaUrl = CreateRobokassaPayment(payment, order);
                confirmationUrl = robokassaUrl;
                payment.SetConfirmationUrl(robokassaUrl);
                break;

            case PaymentProvider.TelegramStars:
                var starsResult = await CreateTelegramStarsPaymentAsync(payment, order, cancellationToken);
                if (starsResult == null)
                    return Result.Failure<PaymentResultDto>("Failed to create Telegram Stars invoice", "PAYMENT_ERROR");
                confirmationUrl = starsResult.InvoiceLink;
                payment.SetConfirmationUrl(starsResult.InvoiceLink);
                payment.SetMetadata(JsonSerializer.Serialize(new { payload = starsResult.Payload }));
                break;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PaymentResultDto(
            payment.Id,
            orderId,
            payment.Status,
            confirmationUrl,
            "Payment created successfully"));
    }

    public async Task<Result<PaymentDto>> GetPaymentStatusAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure<PaymentDto>("Payment not found", "NOT_FOUND");

        // If pending, check with provider
        if (payment.Status == PaymentStatus.Pending && !string.IsNullOrEmpty(payment.ExternalId))
        {
            if (payment.Provider == PaymentProvider.YooKassa)
            {
                var yooKassaPayment = await _yooKassaClient.GetPaymentAsync(payment.ExternalId, cancellationToken);
                if (yooKassaPayment != null)
                {
                    UpdatePaymentStatus(payment, yooKassaPayment.Status);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return Result.Success(MapToDto(payment));
    }

    public async Task<Result> ProcessYooKassaWebhookAsync(YooKassaWebhookDto webhook, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing YooKassa webhook: {Event} for payment {PaymentId}",
            webhook.Event, webhook.Object.Id);

        var payment = await _unitOfWork.Payments.GetByExternalIdAsync(webhook.Object.Id, cancellationToken);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found for YooKassa ID: {ExternalId}", webhook.Object.Id);
            return Result.Failure("Payment not found", "NOT_FOUND");
        }

        UpdatePaymentStatus(payment, webhook.Object.Status);

        if (payment.Status == PaymentStatus.Completed)
        {
            await _orderService.MarkAsPaidAsync(payment.OrderId, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ProcessRobokassaCallbackAsync(RobokassaCallbackDto callback, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Robokassa callback for invoice: {InvId}", callback.InvId);

        var shpParams = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(callback.Shp_orderId))
            shpParams["Shp_orderId"] = callback.Shp_orderId;

        if (!_robokassaClient.ValidateResultSignature(callback.OutSum, callback.InvId, callback.SignatureValue, shpParams))
        {
            _logger.LogWarning("Invalid Robokassa signature for invoice: {InvId}", callback.InvId);
            return Result.Failure("Invalid signature", "INVALID_SIGNATURE");
        }

        if (!Guid.TryParse(callback.Shp_orderId, out var orderId))
            return Result.Failure("Invalid order ID", "INVALID_ORDER");

        var payment = await _unitOfWork.Payments.GetByOrderIdAsync(orderId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "NOT_FOUND");

        payment.SetExternalId(callback.InvId.ToString());
        payment.MarkAsCompleted();

        await _orderService.MarkAsPaidAsync(orderId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ProcessTelegramStarsPaymentAsync(TelegramStarsPaymentDto paymentDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Telegram Stars payment: {Payload}", paymentDto.Payload);

        // Parse payload to get order ID
        if (!Guid.TryParse(paymentDto.Payload, out var orderId))
            return Result.Failure("Invalid payment payload", "INVALID_PAYLOAD");

        var payment = await _unitOfWork.Payments.GetByOrderIdAsync(orderId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "NOT_FOUND");

        payment.SetExternalId(paymentDto.TelegramPaymentChargeId ?? paymentDto.InvoiceId);
        payment.MarkAsCompleted();

        await _orderService.MarkAsPaidAsync(orderId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RefundPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "NOT_FOUND");

        if (payment.Status != PaymentStatus.Completed)
            return Result.Failure("Can only refund completed payments", "INVALID_STATUS");

        payment.MarkAsRefunding();

        bool refundSuccess = false;

        switch (payment.Provider)
        {
            case PaymentProvider.YooKassa:
                var refundResult = await _yooKassaClient.CreateRefundAsync(
                    new YooKassaRefundRequest
                    {
                        PaymentId = payment.ExternalId!,
                        Amount = new YooKassa.YooKassaAmount
                        {
                            Value = payment.Amount.ToString("F2"),
                            Currency = payment.Currency
                        }
                    },
                    Guid.NewGuid().ToString(),
                    cancellationToken);
                refundSuccess = refundResult != null && refundResult.Status == "succeeded";
                break;

            case PaymentProvider.TelegramStars:
                var order = await _unitOfWork.Orders.GetWithDetailsAsync(payment.OrderId, cancellationToken);
                if (order != null)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(order.BuyerId, cancellationToken);
                    if (user != null && !string.IsNullOrEmpty(payment.ExternalId))
                    {
                        refundSuccess = await _telegramStarsClient.RefundStarPaymentAsync(
                            user.TelegramId,
                            payment.ExternalId,
                            cancellationToken);
                    }
                }
                break;

            case PaymentProvider.Robokassa:
                // Robokassa refunds are typically handled manually
                _logger.LogWarning("Robokassa refunds must be processed manually for payment: {PaymentId}", paymentId);
                return Result.Failure("Robokassa refunds must be processed manually", "MANUAL_REFUND_REQUIRED");
        }

        if (refundSuccess)
        {
            payment.MarkAsRefunded();
            var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId, cancellationToken);
            order?.MarkAsRefunded();
        }
        else
        {
            payment.MarkAsCompleted(); // Revert to completed on failure
            return Result.Failure("Refund failed", "REFUND_FAILED");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<YooKassaPaymentResponse?> CreateYooKassaPaymentAsync(
        Payment payment,
        Order order,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var request = new YooKassaPaymentRequest
        {
            Amount = new YooKassa.YooKassaAmount
            {
                Value = payment.Amount.ToString("F2"),
                Currency = payment.Currency
            },
            Description = $"Заказ #{order.Id.ToString()[..8]}",
            Confirmation = new YooKassaConfirmation
            {
                Type = "redirect",
                ReturnUrl = returnUrl
            },
            Capture = true,
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = order.Id.ToString(),
                ["paymentId"] = payment.Id.ToString()
            }
        };

        return await _yooKassaClient.CreatePaymentAsync(request, payment.Id.ToString(), cancellationToken);
    }

    private string CreateRobokassaPayment(Payment payment, Order order)
    {
        var request = new RobokassaPaymentRequest
        {
            Amount = payment.Amount,
            InvoiceId = Math.Abs(payment.Id.GetHashCode()),
            Description = $"Заказ #{order.Id.ToString()[..8]}",
            CustomParams = new Dictionary<string, string>
            {
                ["Shp_orderId"] = order.Id.ToString()
            }
        };

        return _robokassaClient.GeneratePaymentUrl(request);
    }

    private async Task<TelegramInvoiceResponse?> CreateTelegramStarsPaymentAsync(
        Payment payment,
        Order order,
        CancellationToken cancellationToken)
    {
        // Convert RUB to Stars (example rate: 1 Star = ~1.5 RUB)
        var starsAmount = (int)Math.Ceiling(payment.Amount / 1.5m);

        var request = new TelegramInvoiceRequest
        {
            Title = $"Заказ #{order.Id.ToString()[..8]}",
            Description = $"Оплата заказа на сумму {payment.Amount:N0} ₽",
            Payload = order.Id.ToString(),
            Amount = starsAmount
        };

        return await _telegramStarsClient.CreateInvoiceLinkAsync(request, cancellationToken);
    }

    private void UpdatePaymentStatus(Payment payment, string providerStatus)
    {
        switch (providerStatus.ToLowerInvariant())
        {
            case "pending":
                // Keep pending
                break;
            case "waiting_for_capture":
                payment.MarkAsWaitingForCapture();
                break;
            case "succeeded":
                payment.MarkAsCompleted();
                break;
            case "canceled":
                payment.MarkAsCancelled();
                break;
            default:
                _logger.LogWarning("Unknown payment status: {Status}", providerStatus);
                break;
        }
    }

    private PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.OrderId,
            payment.Amount,
            payment.Currency,
            payment.Provider,
            payment.Status,
            payment.ExternalId,
            payment.ConfirmationUrl,
            payment.CreatedAt,
            payment.CompletedAt);
    }
}
