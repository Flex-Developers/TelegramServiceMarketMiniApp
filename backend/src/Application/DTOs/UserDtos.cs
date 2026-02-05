using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Application.DTOs;

public record UserDto(
    Guid Id,
    long TelegramId,
    string? Username,
    string FirstName,
    string? LastName,
    string? PhotoUrl,
    UserRole Role,
    bool IsVerified,
    string? LanguageCode,
    DateTime CreatedAt,
    DateTime? LastActiveAt
);

public record UserProfileDto(
    Guid Id,
    long TelegramId,
    string? Username,
    string FirstName,
    string? LastName,
    string? PhotoUrl,
    UserRole Role,
    bool IsVerified,
    int TotalOrders,
    int TotalServices,
    decimal TotalRevenue,
    decimal AverageRating,
    int ReviewCount,
    DateTime CreatedAt
);

public record SellerProfileDto(
    Guid Id,
    string? Username,
    string FirstName,
    string? LastName,
    string? PhotoUrl,
    bool IsVerified,
    int TotalServices,
    int CompletedOrders,
    decimal AverageRating,
    int ReviewCount,
    int ResponseTimeHours,
    DateTime MemberSince
);

public record CreateUserRequest(
    long TelegramId,
    string FirstName,
    string? LastName,
    string? Username,
    string? PhotoUrl,
    string? LanguageCode
);

public record UpdateUserRequest(
    string FirstName,
    string? LastName,
    string? Username,
    string? PhotoUrl,
    string? LanguageCode
);
