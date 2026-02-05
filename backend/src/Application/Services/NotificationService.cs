using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Enums;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotificationService _realtimeService;

    public NotificationService(IUnitOfWork unitOfWork, IRealtimeNotificationService realtimeService)
    {
        _unitOfWork = unitOfWork;
        _realtimeService = realtimeService;
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetUserNotificationsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (notifications, totalCount) = await _unitOfWork.Notifications.GetByUserIdPagedAsync(userId, page, pageSize, cancellationToken);
        var dtos = notifications.Select(MapToDto).ToList();
        return Result.Success(new PagedResult<NotificationDto>(dtos, totalCount, page, pageSize));
    }

    public async Task<Result<NotificationSummaryDto>> GetNotificationSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _unitOfWork.Notifications.GetUnreadByUserIdAsync(userId, cancellationToken);
        var recent = unreadNotifications.Take(10).Select(MapToDto).ToList();

        return Result.Success(new NotificationSummaryDto(unreadNotifications.Count, recent));
    }

    public async Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken);
        if (notification == null)
            return Result.Failure("Notification not found", "NOT_FOUND");

        if (notification.UserId != userId)
            return Result.Failure("Access denied", "FORBIDDEN");

        notification.MarkAsRead();
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task SendOrderNotificationAsync(Guid userId, NotificationType type, string title, string message, string? data, CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(userId, type, title, message, data);
        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send real-time notification via SignalR
        await _realtimeService.SendNotificationAsync(userId, MapToDto(notification));
    }

    public async Task SendReviewNotificationAsync(Guid sellerId, string buyerName, int rating, CancellationToken cancellationToken = default)
    {
        var stars = new string('⭐', rating);
        var message = $"{buyerName} оставил отзыв {stars}";

        var notification = Notification.Create(sellerId, NotificationType.NewReview, "Новый отзыв", message, null);
        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _realtimeService.SendNotificationAsync(sellerId, MapToDto(notification));
    }

    private NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.Data,
            notification.IsRead,
            notification.CreatedAt,
            notification.ReadAt);
    }
}

public interface IRealtimeNotificationService
{
    Task SendNotificationAsync(Guid userId, NotificationDto notification);
    Task SendOrderUpdateAsync(Guid userId, OrderDto order);
    Task SendPaymentUpdateAsync(Guid userId, PaymentDto payment);
}
