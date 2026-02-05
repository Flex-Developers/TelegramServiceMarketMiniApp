using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Application.DTOs;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? Data,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt
);

public record NotificationSummaryDto(
    int UnreadCount,
    List<NotificationDto> Recent
);

public record CreateNotificationRequest(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Message,
    string? Data
);
