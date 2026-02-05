using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        var rootCategories = categories.Where(c => c.ParentId == null).OrderBy(c => c.SortOrder).ToList();

        var dtos = rootCategories.Select(c => MapToDtoWithChildren(c, categories)).ToList();
        return Result.Success<IReadOnlyList<CategoryDto>>(dtos);
    }

    public async Task<Result<IReadOnlyList<CategoryListDto>>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetRootCategoriesAsync(cancellationToken);
        var dtos = categories.Select(c => new CategoryListDto(
            c.Id,
            c.Name,
            c.NameEn,
            c.NameDe,
            c.Icon,
            c.ImageUrl,
            c.Services.Count(s => s.IsActive)
        )).ToList();

        return Result.Success<IReadOnlyList<CategoryListDto>>(dtos);
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetWithChildrenAsync(id, cancellationToken);
        if (category == null)
            return Result.Failure<CategoryDto>("Category not found", "NOT_FOUND");

        var allCategories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        return Result.Success(MapToDtoWithChildren(category, allCategories));
    }

    public async Task<Result<IReadOnlyList<CategoryListDto>>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var children = await _unitOfWork.Categories.GetChildrenAsync(parentId, cancellationToken);
        var dtos = children.Select(c => new CategoryListDto(
            c.Id,
            c.Name,
            c.NameEn,
            c.NameDe,
            c.Icon,
            c.ImageUrl,
            c.Services.Count(s => s.IsActive)
        )).ToList();

        return Result.Success<IReadOnlyList<CategoryListDto>>(dtos);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ParentId.HasValue)
        {
            var parent = await _unitOfWork.Categories.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent == null)
                return Result.Failure<CategoryDto>("Parent category not found", "INVALID_PARENT");
        }

        var category = Category.Create(
            request.Name,
            request.NameEn,
            request.NameDe,
            request.Icon,
            request.ImageUrl,
            request.ParentId,
            request.SortOrder);

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(category));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null)
            return Result.Failure<CategoryDto>("Category not found", "NOT_FOUND");

        category.Update(request.Name, request.NameEn, request.NameDe, request.Icon, request.ImageUrl, request.SortOrder);
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(category));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetWithChildrenAsync(id, cancellationToken);
        if (category == null)
            return Result.Failure("Category not found", "NOT_FOUND");

        if (category.Children.Any())
            return Result.Failure("Cannot delete category with children", "HAS_CHILDREN");

        var hasServices = await _unitOfWork.Services.AnyAsync(s => s.CategoryId == id, cancellationToken);
        if (hasServices)
            return Result.Failure("Cannot delete category with services", "HAS_SERVICES");

        _unitOfWork.Categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private CategoryDto MapToDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.NameEn,
            category.NameDe,
            category.Icon,
            category.ImageUrl,
            category.ParentId,
            category.SortOrder,
            category.IsActive,
            category.Services.Count(s => s.IsActive),
            null);
    }

    private CategoryDto MapToDtoWithChildren(Category category, IEnumerable<Category> allCategories)
    {
        var children = allCategories
            .Where(c => c.ParentId == category.Id)
            .OrderBy(c => c.SortOrder)
            .Select(c => MapToDtoWithChildren(c, allCategories))
            .ToList();

        return new CategoryDto(
            category.Id,
            category.Name,
            category.NameEn,
            category.NameDe,
            category.Icon,
            category.ImageUrl,
            category.ParentId,
            category.SortOrder,
            category.IsActive,
            category.Services.Count(s => s.IsActive),
            children.Any() ? children : null);
    }
}
