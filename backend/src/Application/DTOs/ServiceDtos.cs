using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Application.DTOs;

public record ServiceDto(
    Guid Id,
    Guid SellerId,
    SellerSummaryDto Seller,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    decimal Price,
    PriceType PriceType,
    int DeliveryDays,
    bool IsActive,
    int ViewCount,
    int OrderCount,
    decimal AverageRating,
    int ReviewCount,
    int ResponseTimeHours,
    List<ServiceImageDto> Images,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ServiceListDto(
    Guid Id,
    string Title,
    decimal Price,
    PriceType PriceType,
    int DeliveryDays,
    decimal AverageRating,
    int ReviewCount,
    string? ThumbnailUrl,
    SellerSummaryDto Seller
);

public record ServiceImageDto(
    Guid Id,
    string ImageUrl,
    string? ThumbnailUrl,
    int SortOrder,
    bool IsPrimary
);

public record SellerSummaryDto(
    Guid Id,
    string? Username,
    string FirstName,
    string? PhotoUrl,
    bool IsVerified,
    decimal AverageRating
);

public record CreateServiceRequest(
    string Title,
    string Description,
    Guid CategoryId,
    decimal Price,
    PriceType PriceType,
    int DeliveryDays,
    int ResponseTimeHours,
    List<string> ImageUrls,
    List<string>? Tags
);

public record UpdateServiceRequest(
    string Title,
    string Description,
    Guid CategoryId,
    decimal Price,
    PriceType PriceType,
    int DeliveryDays,
    int ResponseTimeHours,
    List<string> ImageUrls,
    List<string>? Tags
);

public record ServiceFilterRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    decimal? MinRating = null,
    int? MaxDeliveryDays = null,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false
);
