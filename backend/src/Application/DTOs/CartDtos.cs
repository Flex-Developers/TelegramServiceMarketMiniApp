namespace TelegramMarketplace.Application.DTOs;

public record CartDto(
    List<CartItemDto> Items,
    decimal SubTotal,
    decimal? DiscountAmount,
    string? PromoCode,
    decimal Total,
    int ItemCount
);

public record CartItemDto(
    Guid Id,
    Guid ServiceId,
    string ServiceTitle,
    decimal ServicePrice,
    string? ThumbnailUrl,
    int Quantity,
    decimal TotalPrice,
    SellerSummaryDto Seller
);

public record AddToCartRequest(
    Guid ServiceId,
    int Quantity = 1
);

public record UpdateCartItemRequest(
    int Quantity
);

public record ApplyPromoCodeRequest(
    string Code
);

public record PromoCodeResultDto(
    bool IsValid,
    string? Message,
    decimal? DiscountAmount,
    decimal? NewTotal
);
