using TelegramMarketplace.Domain.Common;

namespace TelegramMarketplace.Domain.Entities;

public class Favorite : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ServiceId { get; private set; }
    public DateTime AddedAt { get; private set; }

    // Navigation properties
    public virtual User User { get; private set; } = null!;
    public virtual Service Service { get; private set; } = null!;

    private Favorite() { }

    public static Favorite Create(Guid userId, Guid serviceId)
    {
        return new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = serviceId,
            AddedAt = DateTime.UtcNow
        };
    }
}
