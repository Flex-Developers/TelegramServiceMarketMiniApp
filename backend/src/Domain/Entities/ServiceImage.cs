using TelegramMarketplace.Domain.Common;

namespace TelegramMarketplace.Domain.Entities;

public class ServiceImage : BaseEntity
{
    public Guid ServiceId { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation property
    public virtual Service Service { get; private set; } = null!;

    private ServiceImage() { }

    public static ServiceImage Create(Guid serviceId, string imageUrl, int sortOrder, bool isPrimary = false, string? thumbnailUrl = null)
    {
        return new ServiceImage
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            SortOrder = sortOrder,
            IsPrimary = isPrimary
        };
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void UnsetAsPrimary()
    {
        IsPrimary = false;
    }

    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
