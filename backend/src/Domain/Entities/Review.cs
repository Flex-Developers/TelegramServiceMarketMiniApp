using TelegramMarketplace.Domain.Common;

namespace TelegramMarketplace.Domain.Entities;

public class Review : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public Guid SellerId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public List<string> Images { get; private set; } = new();
    public string? SellerResponse { get; private set; }
    public DateTime? ResponseDate { get; private set; }
    public int HelpfulVotes { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public virtual Order Order { get; private set; } = null!;
    public virtual Service Service { get; private set; } = null!;
    public virtual User Reviewer { get; private set; } = null!;
    public virtual User Seller { get; private set; } = null!;

    private Review() { }

    public static Review Create(
        Guid orderId,
        Guid serviceId,
        Guid reviewerId,
        Guid sellerId,
        int rating,
        string? comment = null,
        List<string>? images = null)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        return new Review
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ServiceId = serviceId,
            ReviewerId = reviewerId,
            SellerId = sellerId,
            Rating = rating,
            Comment = comment,
            Images = images ?? new List<string>(),
            HelpfulVotes = 0,
            IsVerifiedPurchase = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(int rating, string? comment, List<string>? images = null)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        Rating = rating;
        Comment = comment;
        if (images != null)
            Images = images;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSellerResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentException("Response cannot be empty", nameof(response));

        SellerResponse = response;
        ResponseDate = DateTime.UtcNow;
    }

    public void IncrementHelpfulVotes()
    {
        HelpfulVotes++;
    }
}
