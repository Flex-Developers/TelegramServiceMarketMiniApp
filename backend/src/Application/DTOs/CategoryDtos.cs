namespace TelegramMarketplace.Application.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string NameEn,
    string NameDe,
    string? Icon,
    string? ImageUrl,
    Guid? ParentId,
    int SortOrder,
    bool IsActive,
    int ServiceCount,
    List<CategoryDto>? Children
);

public record CategoryListDto(
    Guid Id,
    string Name,
    string NameEn,
    string NameDe,
    string? Icon,
    string? ImageUrl,
    int ServiceCount
);

public record CreateCategoryRequest(
    string Name,
    string NameEn,
    string NameDe,
    string? Icon,
    string? ImageUrl,
    Guid? ParentId,
    int SortOrder
);

public record UpdateCategoryRequest(
    string Name,
    string NameEn,
    string NameDe,
    string? Icon,
    string? ImageUrl,
    int SortOrder
);
