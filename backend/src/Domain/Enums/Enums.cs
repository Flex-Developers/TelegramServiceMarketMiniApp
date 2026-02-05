namespace TelegramMarketplace.Domain.Enums;

public enum UserRole
{
    Buyer = 0,
    Seller = 1,
    Both = 2,
    Admin = 3
}

public enum PriceType
{
    Fixed = 0,
    Hourly = 1
}

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Processing = 2,
    Delivered = 3,
    Completed = 4,
    Cancelled = 5,
    Refunded = 6,
    Disputed = 7
}

public enum PaymentStatus
{
    Pending = 0,
    WaitingForCapture = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Refunding = 5,
    Refunded = 6
}

public enum PaymentMethod
{
    YooKassa = 0,
    Robokassa = 1,
    TelegramStars = 2
}

public enum PaymentProvider
{
    YooKassa = 0,
    Robokassa = 1,
    TelegramStars = 2
}

public enum NotificationType
{
    OrderCreated = 0,
    OrderPaid = 1,
    OrderProcessing = 2,
    OrderDelivered = 3,
    OrderCompleted = 4,
    OrderCancelled = 5,
    NewReview = 6,
    NewMessage = 7,
    PaymentReceived = 8,
    PayoutProcessed = 9
}

public enum DiscountType
{
    Percentage = 0,
    Fixed = 1
}
