using TelegramMarketplace.Domain.Common;

namespace TelegramMarketplace.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameDe { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public string? ImageUrl { get; private set; }
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public virtual Category? Parent { get; private set; }
    public virtual ICollection<Category> Children { get; private set; } = new List<Category>();
    public virtual ICollection<Service> Services { get; private set; } = new List<Service>();

    private Category() { }

    public static Category Create(
        string name,
        string nameEn,
        string nameDe,
        string? icon = null,
        string? imageUrl = null,
        Guid? parentId = null,
        int sortOrder = 0)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameEn = nameEn,
            NameDe = nameDe,
            Icon = icon,
            ImageUrl = imageUrl,
            ParentId = parentId,
            SortOrder = sortOrder,
            IsActive = true
        };
    }

    public void Update(string name, string nameEn, string nameDe, string? icon, string? imageUrl, int sortOrder)
    {
        Name = name;
        NameEn = nameEn;
        NameDe = nameDe;
        Icon = icon;
        ImageUrl = imageUrl;
        SortOrder = sortOrder;
    }

    public void SetParent(Guid? parentId)
    {
        ParentId = parentId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
