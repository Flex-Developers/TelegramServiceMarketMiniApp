using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Application.Services;

public interface IPaymentService
{
    Task<Result<PaymentResultDto>> CreatePaymentAsync(Guid orderId, PaymentProvider provider, string? returnUrl, CancellationToken cancellationToken = default);
    Task<Result<PaymentDto>> GetPaymentStatusAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Result> ProcessYooKassaWebhookAsync(YooKassaWebhookDto webhook, CancellationToken cancellationToken = default);
    Task<Result> ProcessRobokassaCallbackAsync(RobokassaCallbackDto callback, CancellationToken cancellationToken = default);
    Task<Result> ProcessTelegramStarsPaymentAsync(TelegramStarsPaymentDto payment, CancellationToken cancellationToken = default);
    Task<Result> RefundPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
}
