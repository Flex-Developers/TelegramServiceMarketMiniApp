using TelegramMarketplace.Domain.Common;
using TelegramMarketplace.Domain.Enums;
using TelegramMarketplace.Domain.Events;

namespace TelegramMarketplace.Domain.Entities;

public class Order : BaseEntity
{
    public Guid BuyerId { get; private set; }
    public Guid SellerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal Commission { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public string? PromoCode { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navigation properties
    public virtual User Buyer { get; private set; } = null!;
    public virtual User Seller { get; private set; } = null!;
    public virtual ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public virtual Payment? Payment { get; private set; }
    public virtual Review? Review { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Order() { }

    public static Order Create(
        Guid buyerId,
        Guid sellerId,
        decimal subTotal,
        decimal commissionPercentage,
        PaymentMethod paymentMethod,
        string? promoCode = null,
        decimal discountAmount = 0,
        string? notes = null)
    {
        var commission = subTotal * (commissionPercentage / 100);
        var totalAmount = subTotal - discountAmount;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            SellerId = sellerId,
            Status = OrderStatus.Pending,
            SubTotal = subTotal,
            Commission = commission,
            TotalAmount = totalAmount,
            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            PromoCode = promoCode,
            DiscountAmount = discountAmount,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        order._domainEvents.Add(new OrderCreatedEvent(order.Id, buyerId, sellerId, totalAmount));
        return order;
    }

    public void AddItem(OrderItem item)
    {
        Items.Add(item);
    }

    public void MarkAsPaid()
    {
        PaymentStatus = PaymentStatus.Completed;
        Status = OrderStatus.Paid;
        PaidAt = DateTime.UtcNow;
        _domainEvents.Add(new OrderPaidEvent(Id, BuyerId, SellerId, TotalAmount));
    }

    public void MarkAsProcessing()
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException("Order must be paid before processing");

        Status = OrderStatus.Processing;
        _domainEvents.Add(new OrderStatusChangedEvent(Id, Status));
    }

    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException("Order must be processing before delivery");

        Status = OrderStatus.Delivered;
        _domainEvents.Add(new OrderStatusChangedEvent(Id, Status));
    }

    public void Complete()
    {
        if (Status != OrderStatus.Delivered)
            throw new InvalidOperationException("Order must be delivered before completion");

        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        _domainEvents.Add(new OrderCompletedEvent(Id, BuyerId, SellerId));
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed order");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        _domainEvents.Add(new OrderCancelledEvent(Id, BuyerId, SellerId, reason));
    }

    public void SetPaymentFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
    }

    public void RequestRefund()
    {
        if (PaymentStatus != PaymentStatus.Completed)
            throw new InvalidOperationException("Can only refund completed payments");

        PaymentStatus = PaymentStatus.Refunding;
    }

    public void MarkAsRefunded()
    {
        PaymentStatus = PaymentStatus.Refunded;
        Status = OrderStatus.Refunded;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
