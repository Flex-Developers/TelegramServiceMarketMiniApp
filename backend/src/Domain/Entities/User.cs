using TelegramMarketplace.Domain.Common;
using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Domain.Entities;

public class User : BaseEntity
{
    public long TelegramId { get; private set; }
    public string? Username { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public string? PhotoUrl { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsVerified { get; private set; }
    public string? LanguageCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastActiveAt { get; private set; }

    // Navigation properties
    public virtual ICollection<Service> Services { get; private set; } = new List<Service>();
    public virtual ICollection<Order> BuyerOrders { get; private set; } = new List<Order>();
    public virtual ICollection<Order> SellerOrders { get; private set; } = new List<Order>();
    public virtual ICollection<CartItem> CartItems { get; private set; } = new List<CartItem>();
    public virtual ICollection<Favorite> Favorites { get; private set; } = new List<Favorite>();
    public virtual ICollection<Review> ReviewsWritten { get; private set; } = new List<Review>();
    public virtual ICollection<Review> ReviewsReceived { get; private set; } = new List<Review>();
    public virtual ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

    private User() { }

    public static User Create(
        long telegramId,
        string firstName,
        string? lastName = null,
        string? username = null,
        string? photoUrl = null,
        string? languageCode = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            FirstName = firstName,
            LastName = lastName,
            Username = username,
            PhotoUrl = photoUrl,
            LanguageCode = languageCode ?? "ru",
            Role = UserRole.Buyer,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string firstName, string? lastName, string? username, string? photoUrl)
    {
        FirstName = firstName;
        LastName = lastName;
        Username = username;
        PhotoUrl = photoUrl;
        LastActiveAt = DateTime.UtcNow;
    }

    public void BecomeSeller()
    {
        Role = Role == UserRole.Buyer ? UserRole.Both : Role;
    }

    public void Verify()
    {
        IsVerified = true;
    }

    public void UpdateLastActive()
    {
        LastActiveAt = DateTime.UtcNow;
    }

    public void SetLanguage(string languageCode)
    {
        LanguageCode = languageCode;
    }
}
