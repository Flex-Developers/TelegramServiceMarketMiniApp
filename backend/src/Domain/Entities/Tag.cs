using TelegramMarketplace.Domain.Common;

namespace TelegramMarketplace.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    // Navigation property
    public virtual ICollection<ServiceTag> ServiceTags { get; private set; } = new List<ServiceTag>();

    private Tag() { }

    public static Tag Create(string name)
    {
        return new Tag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = GenerateSlug(name)
        };
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
    }
}
