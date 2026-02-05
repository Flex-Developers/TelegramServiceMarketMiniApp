namespace TelegramMarketplace.Application.DTOs;

public record ReviewDto(
    Guid Id,
    Guid OrderId,
    Guid ServiceId,
    Guid ReviewerId,
    UserSummaryDto Reviewer,
    int Rating,
    string? Comment,
    List<string> Images,
    string? SellerResponse,
    DateTime? ResponseDate,
    int HelpfulVotes,
    bool IsVerifiedPurchase,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ReviewListDto(
    Guid Id,
    UserSummaryDto Reviewer,
    int Rating,
    string? Comment,
    List<string> Images,
    string? SellerResponse,
    int HelpfulVotes,
    bool IsVerifiedPurchase,
    DateTime CreatedAt
);

public record ReviewStatsDto(
    decimal AverageRating,
    int TotalReviews,
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount
);

public record CreateReviewRequest(
    Guid OrderId,
    int Rating,
    string? Comment,
    List<string>? Images
);

public record UpdateReviewRequest(
    int Rating,
    string? Comment,
    List<string>? Images
);

public record SellerResponseRequest(
    string Response
);
