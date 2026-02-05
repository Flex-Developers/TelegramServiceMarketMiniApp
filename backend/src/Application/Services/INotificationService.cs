using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Application.Services;

public interface INotificationService
{
    Task<Result<PagedResult<NotificationDto>>> GetUserNotificationsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<NotificationSummaryDto>> GetNotificationSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SendOrderNotificationAsync(Guid userId, NotificationType type, string title, string message, string? data, CancellationToken cancellationToken = default);
    Task SendReviewNotificationAsync(Guid sellerId, string buyerName, int rating, CancellationToken cancellationToken = default);
}
