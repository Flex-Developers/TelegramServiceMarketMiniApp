using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid BuyerId { get; }
    public Guid SellerId { get; }
    public decimal TotalAmount { get; }

    public OrderCreatedEvent(Guid orderId, Guid buyerId, Guid sellerId, decimal totalAmount)
    {
        OrderId = orderId;
        BuyerId = buyerId;
        SellerId = sellerId;
        TotalAmount = totalAmount;
    }
}

public class OrderPaidEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid BuyerId { get; }
    public Guid SellerId { get; }
    public decimal Amount { get; }

    public OrderPaidEvent(Guid orderId, Guid buyerId, Guid sellerId, decimal amount)
    {
        OrderId = orderId;
        BuyerId = buyerId;
        SellerId = sellerId;
        Amount = amount;
    }
}

public class OrderStatusChangedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public OrderStatus NewStatus { get; }

    public OrderStatusChangedEvent(Guid orderId, OrderStatus newStatus)
    {
        OrderId = orderId;
        NewStatus = newStatus;
    }
}

public class OrderCompletedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid BuyerId { get; }
    public Guid SellerId { get; }

    public OrderCompletedEvent(Guid orderId, Guid buyerId, Guid sellerId)
    {
        OrderId = orderId;
        BuyerId = buyerId;
        SellerId = sellerId;
    }
}

public class OrderCancelledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid BuyerId { get; }
    public Guid SellerId { get; }
    public string Reason { get; }

    public OrderCancelledEvent(Guid orderId, Guid buyerId, Guid sellerId, string reason)
    {
        OrderId = orderId;
        BuyerId = buyerId;
        SellerId = sellerId;
        Reason = reason;
    }
}

public class PaymentCompletedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public decimal Amount { get; }

    public PaymentCompletedEvent(Guid paymentId, Guid orderId, decimal amount)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
    }
}

public class ReviewCreatedEvent : DomainEvent
{
    public Guid ReviewId { get; }
    public Guid ServiceId { get; }
    public Guid SellerId { get; }
    public int Rating { get; }

    public ReviewCreatedEvent(Guid reviewId, Guid serviceId, Guid sellerId, int rating)
    {
        ReviewId = reviewId;
        ServiceId = serviceId;
        SellerId = sellerId;
        Rating = rating;
    }
}
