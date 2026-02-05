using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IUnitOfWork _unitOfWork;

    public CategoriesController(ICategoryService categoryService, IUnitOfWork unitOfWork)
    {
        _categoryService = categoryService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get all categories with hierarchy
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllCategoriesAsync(cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get root categories only
    /// </summary>
    [HttpGet("root")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRootCategories(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetRootCategoriesAsync(cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get subcategories of a category
    /// </summary>
    [HttpGet("{id:guid}/children")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChildren(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetChildrenAsync(id, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    // ============ ADMIN ENDPOINTS ============

    /// <summary>
    /// Create a new category (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = Category.Create(
            request.Name,
            request.NameEn ?? request.Name,
            request.NameDe ?? request.Name,
            request.Icon,
            request.ImageUrl,
            request.ParentId,
            request.SortOrder);

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new CategoryDto(
            category.Id,
            category.Name,
            category.NameEn,
            category.NameDe,
            category.Icon,
            category.ImageUrl,
            category.ParentId,
            category.SortOrder,
            category.IsActive,
            0,
            new List<CategoryDto>()));
    }

    /// <summary>
    /// Update a category (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null)
            return NotFound(new { error = "Category not found" });

        category.Update(
            request.Name,
            request.NameEn ?? request.Name,
            request.NameDe ?? request.Name,
            request.Icon,
            request.ImageUrl,
            request.SortOrder);

        if (request.ParentId != category.ParentId)
            category.SetParent(request.ParentId);

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Category updated" });
    }

    /// <summary>
    /// Delete a category (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null)
            return NotFound(new { error = "Category not found" });

        _unitOfWork.Categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Category deleted" });
    }

    /// <summary>
    /// Activate a category (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateCategory(Guid id, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null)
            return NotFound(new { error = "Category not found" });

        category.Activate();
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Category activated" });
    }

    /// <summary>
    /// Deactivate a category (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCategory(Guid id, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null)
            return NotFound(new { error = "Category not found" });

        category.Deactivate();
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Category deactivated" });
    }
}

// Request DTOs
public record CreateCategoryRequest(
    string Name,
    string? NameEn,
    string? NameDe,
    string? Icon,
    string? ImageUrl,
    Guid? ParentId,
    int SortOrder = 0);

public record UpdateCategoryRequest(
    string Name,
    string? NameEn,
    string? NameDe,
    string? Icon,
    string? ImageUrl,
    Guid? ParentId,
    int SortOrder = 0);
