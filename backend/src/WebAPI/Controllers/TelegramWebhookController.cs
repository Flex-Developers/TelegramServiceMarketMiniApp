using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.Domain.Interfaces;
using TelegramMarketplace.Infrastructure.Payments.TelegramStars;

namespace TelegramMarketplace.WebAPI.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly ITelegramStarsClient _telegramStarsClient;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TelegramWebhookController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public TelegramWebhookController(
        ITelegramStarsClient telegramStarsClient,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        ILogger<TelegramWebhookController> logger)
    {
        _telegramStarsClient = telegramStarsClient;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Telegram Bot webhook endpoint
    /// Handles pre_checkout_query and successful_payment updates
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        _logger.LogInformation("Received Telegram webhook: {Body}", body);

        try
        {
            var update = JsonSerializer.Deserialize<TelegramWebhookUpdate>(body, JsonOptions);

            if (update == null)
            {
                _logger.LogWarning("Failed to deserialize Telegram update");
                return Ok(); // Always return OK to Telegram
            }

            // Handle pre-checkout query - MUST respond within 10 seconds
            if (update.PreCheckoutQuery != null)
            {
                await HandlePreCheckoutQueryAsync(update.PreCheckoutQuery, cancellationToken);
            }

            // Handle successful payment
            if (update.Message?.SuccessfulPayment != null)
            {
                await HandleSuccessfulPaymentAsync(update.Message.SuccessfulPayment, cancellationToken);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram webhook");
            return Ok(); // Always return OK to prevent Telegram from retrying
        }
    }

    private async Task HandlePreCheckoutQueryAsync(TelegramPreCheckoutQueryUpdate query, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing pre-checkout query: Id={QueryId}, Payload={Payload}, Amount={Amount}",
            query.Id, query.InvoicePayload, query.TotalAmount);

        try
        {
            // Validate the order exists and is pending payment
            if (Guid.TryParse(query.InvoicePayload, out var orderId))
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for pre-checkout: {OrderId}", orderId);
                    await _telegramStarsClient.AnswerPreCheckoutQueryAsync(
                        query.Id,
                        false,
                        "Заказ не найден",
                        cancellationToken);
                    return;
                }

                // Order exists, approve the pre-checkout
                var success = await _telegramStarsClient.AnswerPreCheckoutQueryAsync(
                    query.Id,
                    true,
                    null,
                    cancellationToken);

                _logger.LogInformation(
                    "Pre-checkout query answered: QueryId={QueryId}, Success={Success}",
                    query.Id, success);
            }
            else
            {
                _logger.LogWarning("Invalid payload in pre-checkout query: {Payload}", query.InvoicePayload);
                await _telegramStarsClient.AnswerPreCheckoutQueryAsync(
                    query.Id,
                    false,
                    "Неверные данные заказа",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling pre-checkout query {QueryId}", query.Id);
            // Try to decline the pre-checkout on error
            try
            {
                await _telegramStarsClient.AnswerPreCheckoutQueryAsync(
                    query.Id,
                    false,
                    "Ошибка обработки заказа",
                    cancellationToken);
            }
            catch
            {
                // Ignore
            }
        }
    }

    private async Task HandleSuccessfulPaymentAsync(TelegramSuccessfulPaymentUpdate payment, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing successful payment: Payload={Payload}, ChargeId={ChargeId}",
            payment.InvoicePayload, payment.TelegramPaymentChargeId);

        try
        {
            var paymentDto = new TelegramStarsPaymentDto(
                string.Empty,
                payment.TotalAmount,
                payment.InvoicePayload,
                payment.TelegramPaymentChargeId);

            var result = await _paymentService.ProcessTelegramStarsPaymentAsync(paymentDto, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to process successful payment: {Error}", result.Error);
            }
            else
            {
                _logger.LogInformation("Successfully processed Telegram Stars payment for payload: {Payload}", payment.InvoicePayload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing successful payment");
        }
    }
}

// Internal DTOs for webhook deserialization with snake_case
internal class TelegramWebhookUpdate
{
    public long UpdateId { get; set; }
    public TelegramPreCheckoutQueryUpdate? PreCheckoutQuery { get; set; }
    public TelegramMessageUpdate? Message { get; set; }
}

internal class TelegramPreCheckoutQueryUpdate
{
    public string Id { get; set; } = string.Empty;
    public TelegramUserUpdate? From { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int TotalAmount { get; set; }
    public string InvoicePayload { get; set; } = string.Empty;
}

internal class TelegramMessageUpdate
{
    public int MessageId { get; set; }
    public TelegramUserUpdate? From { get; set; }
    public TelegramSuccessfulPaymentUpdate? SuccessfulPayment { get; set; }
}

internal class TelegramUserUpdate
{
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? Username { get; set; }
}

internal class TelegramSuccessfulPaymentUpdate
{
    public string Currency { get; set; } = string.Empty;
    public int TotalAmount { get; set; }
    public string InvoicePayload { get; set; } = string.Empty;
    public string TelegramPaymentChargeId { get; set; } = string.Empty;
    public string ProviderPaymentChargeId { get; set; } = string.Empty;
}
