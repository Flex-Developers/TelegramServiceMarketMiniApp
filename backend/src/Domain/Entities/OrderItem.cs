using TelegramMarketplace.Domain.Common;

namespace TelegramMarketplace.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ServiceId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public string ServiceTitle { get; private set; } = string.Empty;
    public string? ServiceDescription { get; private set; }

    // Navigation properties
    public virtual Order Order { get; private set; } = null!;
    public virtual Service Service { get; private set; } = null!;

    private OrderItem() { }

    public static OrderItem Create(
        Guid orderId,
        Guid serviceId,
        string serviceTitle,
        string? serviceDescription,
        int quantity,
        decimal unitPrice)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ServiceId = serviceId,
            ServiceTitle = serviceTitle,
            ServiceDescription = serviceDescription,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice
        };
    }

    public void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
        TotalPrice = quantity * UnitPrice;
    }
}
