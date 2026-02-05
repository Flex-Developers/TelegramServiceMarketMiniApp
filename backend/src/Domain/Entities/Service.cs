using TelegramMarketplace.Domain.Common;
using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Domain.Entities;

public class Service : BaseEntity
{
    public Guid SellerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public decimal Price { get; private set; }
    public PriceType PriceType { get; private set; }
    public int DeliveryDays { get; private set; }
    public bool IsActive { get; private set; }
    public int ViewCount { get; private set; }
    public int OrderCount { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public int ResponseTimeHours { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public virtual User Seller { get; private set; } = null!;
    public virtual Category Category { get; private set; } = null!;
    public virtual ICollection<ServiceImage> Images { get; private set; } = new List<ServiceImage>();
    public virtual ICollection<ServiceTag> Tags { get; private set; } = new List<ServiceTag>();
    public virtual ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();
    public virtual ICollection<CartItem> CartItems { get; private set; } = new List<CartItem>();
    public virtual ICollection<Favorite> Favorites { get; private set; } = new List<Favorite>();
    public virtual ICollection<Review> Reviews { get; private set; } = new List<Review>();

    private Service() { }

    public static Service Create(
        Guid sellerId,
        string title,
        string description,
        Guid categoryId,
        decimal price,
        PriceType priceType,
        int deliveryDays,
        int responseTimeHours = 24)
    {
        return new Service
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Title = title,
            Description = description,
            CategoryId = categoryId,
            Price = price,
            PriceType = priceType,
            DeliveryDays = deliveryDays,
            ResponseTimeHours = responseTimeHours,
            IsActive = true,
            ViewCount = 0,
            OrderCount = 0,
            AverageRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string description,
        Guid categoryId,
        decimal price,
        PriceType priceType,
        int deliveryDays,
        int responseTimeHours)
    {
        Title = title;
        Description = description;
        CategoryId = categoryId;
        Price = price;
        PriceType = priceType;
        DeliveryDays = deliveryDays;
        ResponseTimeHours = responseTimeHours;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    public void IncrementOrderCount()
    {
        OrderCount++;
    }

    public void UpdateRating(decimal averageRating, int reviewCount)
    {
        AverageRating = averageRating;
        ReviewCount = reviewCount;
    }

    public void AddImage(ServiceImage image)
    {
        Images.Add(image);
    }

    public void ClearImages()
    {
        Images.Clear();
    }
}
