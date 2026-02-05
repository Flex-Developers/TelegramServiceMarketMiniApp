using TelegramMarketplace.Domain.Common;

namespace TelegramMarketplace.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ServiceId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime AddedAt { get; private set; }

    // Navigation properties
    public virtual User User { get; private set; } = null!;
    public virtual Service Service { get; private set; } = null!;

    private CartItem() { }

    public static CartItem Create(Guid userId, Guid serviceId, int quantity = 1)
    {
        return new CartItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = serviceId,
            Quantity = quantity,
            AddedAt = DateTime.UtcNow
        };
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        Quantity = quantity;
    }

    public void IncrementQuantity()
    {
        Quantity++;
    }

    public void DecrementQuantity()
    {
        if (Quantity > 1)
            Quantity--;
    }
}
