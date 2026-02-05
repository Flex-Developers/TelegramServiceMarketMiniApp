using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Enums;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Application.Services;

public class ServiceService : IServiceService
{
    private readonly IUnitOfWork _unitOfWork;

    public ServiceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ServiceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetWithDetailsAsync(id, cancellationToken);
        if (service == null)
            return Result.Failure<ServiceDto>("Service not found", "NOT_FOUND");

        return Result.Success(MapToDto(service));
    }

    public async Task<Result<PagedResult<ServiceListDto>>> GetPagedAsync(ServiceFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _unitOfWork.Services.GetPagedAsync(
            filter.Page,
            filter.PageSize,
            filter.CategoryId,
            filter.MinPrice,
            filter.MaxPrice,
            filter.MinRating,
            filter.MaxDeliveryDays,
            filter.SearchTerm,
            filter.SortBy,
            filter.SortDescending,
            cancellationToken);

        var dtos = items.Select(MapToListDto).ToList();
        return Result.Success(new PagedResult<ServiceListDto>(dtos, totalCount, filter.Page, filter.PageSize));
    }

    public async Task<Result<IReadOnlyList<ServiceListDto>>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var services = await _unitOfWork.Services.GetBySellerIdAsync(sellerId, cancellationToken);
        return Result.Success<IReadOnlyList<ServiceListDto>>(services.Select(MapToListDto).ToList());
    }

    public async Task<Result<IReadOnlyList<ServiceListDto>>> GetFeaturedAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var services = await _unitOfWork.Services.GetFeaturedAsync(count, cancellationToken);
        return Result.Success<IReadOnlyList<ServiceListDto>>(services.Select(MapToListDto).ToList());
    }

    public async Task<Result<ServiceDto>> CreateAsync(Guid sellerId, CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var seller = await _unitOfWork.Users.GetByIdAsync(sellerId, cancellationToken);
        if (seller == null)
            return Result.Failure<ServiceDto>("Seller not found", "NOT_FOUND");

        if (seller.Role != UserRole.Seller && seller.Role != UserRole.Both && seller.Role != UserRole.Admin)
        {
            seller.BecomeSeller();
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
            return Result.Failure<ServiceDto>("Category not found", "INVALID_CATEGORY");

        var service = Service.Create(
            sellerId,
            request.Title,
            request.Description,
            request.CategoryId,
            request.Price,
            request.PriceType,
            request.DeliveryDays,
            request.ResponseTimeHours);

        // Add images
        for (int i = 0; i < request.ImageUrls.Count && i < 10; i++)
        {
            var image = ServiceImage.Create(service.Id, request.ImageUrls[i], i, i == 0);
            service.AddImage(image);
        }

        await _unitOfWork.Services.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdService = await _unitOfWork.Services.GetWithDetailsAsync(service.Id, cancellationToken);
        return Result.Success(MapToDto(createdService!));
    }

    public async Task<Result<ServiceDto>> UpdateAsync(Guid id, Guid sellerId, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetWithDetailsAsync(id, cancellationToken);
        if (service == null)
            return Result.Failure<ServiceDto>("Service not found", "NOT_FOUND");

        if (service.SellerId != sellerId)
            return Result.Failure<ServiceDto>("You don't have permission to update this service", "FORBIDDEN");

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
            return Result.Failure<ServiceDto>("Category not found", "INVALID_CATEGORY");

        service.Update(
            request.Title,
            request.Description,
            request.CategoryId,
            request.Price,
            request.PriceType,
            request.DeliveryDays,
            request.ResponseTimeHours);

        // Update service first (without images)
        _unitOfWork.Services.Update(service);

        // Delete old images from database
        await _unitOfWork.DeleteServiceImagesAsync(service.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Now add new images in a fresh operation
        for (int i = 0; i < request.ImageUrls.Count && i < 10; i++)
        {
            var image = ServiceImage.Create(service.Id, request.ImageUrls[i], i, i == 0);
            await _unitOfWork.AddServiceImageAsync(image, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedService = await _unitOfWork.Services.GetWithDetailsAsync(service.Id, cancellationToken);
        return Result.Success(MapToDto(updatedService!));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null)
            return Result.Failure("Service not found", "NOT_FOUND");

        if (service.SellerId != sellerId)
            return Result.Failure("You don't have permission to delete this service", "FORBIDDEN");

        _unitOfWork.Services.Remove(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ActivateAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null)
            return Result.Failure("Service not found", "NOT_FOUND");

        if (service.SellerId != sellerId)
            return Result.Failure("You don't have permission to modify this service", "FORBIDDEN");

        service.Activate();
        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null)
            return Result.Failure("Service not found", "NOT_FOUND");

        if (service.SellerId != sellerId)
            return Result.Failure("You don't have permission to modify this service", "FORBIDDEN");

        service.Deactivate();
        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null)
            return Result.Failure("Service not found", "NOT_FOUND");

        service.IncrementViewCount();
        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private ServiceDto MapToDto(Service service)
    {
        return new ServiceDto(
            service.Id,
            service.SellerId,
            new SellerSummaryDto(
                service.Seller.Id,
                service.Seller.Username,
                service.Seller.FirstName,
                service.Seller.PhotoUrl,
                service.Seller.IsVerified,
                service.AverageRating),
            service.Title,
            service.Description,
            service.CategoryId,
            service.Category?.Name ?? "",
            service.Price,
            service.PriceType,
            service.DeliveryDays,
            service.IsActive,
            service.ViewCount,
            service.OrderCount,
            service.AverageRating,
            service.ReviewCount,
            service.ResponseTimeHours,
            service.Images.OrderBy(i => i.SortOrder).Select(i => new ServiceImageDto(
                i.Id, i.ImageUrl, i.ThumbnailUrl, i.SortOrder, i.IsPrimary)).ToList(),
            service.Tags.Select(t => t.Tag.Name).ToList(),
            service.CreatedAt,
            service.UpdatedAt);
    }

    private ServiceListDto MapToListDto(Service service)
    {
        var thumbnail = service.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ThumbnailUrl
            ?? service.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl;

        return new ServiceListDto(
            service.Id,
            service.Title,
            service.Price,
            service.PriceType,
            service.DeliveryDays,
            service.AverageRating,
            service.ReviewCount,
            thumbnail,
            new SellerSummaryDto(
                service.Seller.Id,
                service.Seller.Username,
                service.Seller.FirstName,
                service.Seller.PhotoUrl,
                service.Seller.IsVerified,
                service.AverageRating));
    }
}
