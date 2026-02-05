namespace TelegramMarketplace.Application.DTOs;

public record SellerAnalyticsDto(
    decimal TotalRevenue,
    decimal ThisMonthRevenue,
    decimal LastMonthRevenue,
    decimal RevenueGrowthPercent,
    int TotalOrders,
    int ThisMonthOrders,
    int CompletedOrders,
    int PendingOrders,
    decimal AverageOrderValue,
    decimal AverageRating,
    int TotalReviews,
    List<RevenueDataPoint> RevenueChart,
    List<TopServiceDto> TopServices,
    List<OrderStatusBreakdown> OrderStatusBreakdown
);

public record RevenueDataPoint(
    DateTime Date,
    decimal Revenue,
    int OrderCount
);

public record TopServiceDto(
    Guid ServiceId,
    string Title,
    string? ThumbnailUrl,
    int OrderCount,
    decimal Revenue
);

public record OrderStatusBreakdown(
    string Status,
    int Count,
    decimal Percentage
);

public record PlatformAnalyticsDto(
    int TotalUsers,
    int TotalSellers,
    int TotalBuyers,
    int NewUsersThisMonth,
    decimal TotalRevenue,
    decimal TotalCommission,
    int TotalOrders,
    int TotalServices,
    List<RevenueDataPoint> RevenueChart,
    List<CategoryBreakdown> CategoryBreakdown
);

public record CategoryBreakdown(
    Guid CategoryId,
    string CategoryName,
    int ServiceCount,
    int OrderCount,
    decimal Revenue
);
