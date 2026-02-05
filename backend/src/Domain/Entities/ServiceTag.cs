namespace TelegramMarketplace.Domain.Entities;

public class ServiceTag
{
    public Guid ServiceId { get; private set; }
    public Guid TagId { get; private set; }

    // Navigation properties
    public virtual Service Service { get; private set; } = null!;
    public virtual Tag Tag { get; private set; } = null!;

    private ServiceTag() { }

    public static ServiceTag Create(Guid serviceId, Guid tagId)
    {
        return new ServiceTag
        {
            ServiceId = serviceId,
            TagId = tagId
        };
    }
}
