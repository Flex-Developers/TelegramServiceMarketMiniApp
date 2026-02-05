using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Application.DTOs;

public record OrderDto(
    Guid Id,
    Guid BuyerId,
    Guid SellerId,
    UserSummaryDto Buyer,
    UserSummaryDto Seller,
    OrderStatus Status,
    decimal SubTotal,
    decimal Commission,
    decimal TotalAmount,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    string? PromoCode,
    decimal DiscountAmount,
    string? Notes,
    List<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? CancellationReason
);

public record OrderListDto(
    Guid Id,
    OrderStatus Status,
    decimal TotalAmount,
    PaymentStatus PaymentStatus,
    int ItemCount,
    string FirstItemTitle,
    string? FirstItemThumbnail,
    UserSummaryDto OtherParty,
    DateTime CreatedAt
);

public record OrderItemDto(
    Guid Id,
    Guid ServiceId,
    string ServiceTitle,
    string? ServiceDescription,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? ThumbnailUrl
);

public record UserSummaryDto(
    Guid Id,
    string? Username,
    string FirstName,
    string? PhotoUrl
);

public record CreateOrderRequest(
    PaymentMethod PaymentMethod,
    string? PromoCode,
    string? Notes
);

public record UpdateOrderStatusRequest(
    OrderStatus Status,
    string? Notes
);

public record CancelOrderRequest(
    string Reason
);
