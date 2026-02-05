using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Application.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IUnitOfWork _unitOfWork;

    public FavoriteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<ServiceListDto>>> GetUserFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var favorites = await _unitOfWork.Favorites.GetByUserIdAsync(userId, cancellationToken);

        var serviceDtos = favorites.Select(f => new ServiceListDto(
            f.Service.Id,
            f.Service.Title,
            f.Service.Price,
            f.Service.PriceType,
            f.Service.DeliveryDays,
            f.Service.AverageRating,
            f.Service.ReviewCount,
            f.Service.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ThumbnailUrl,
            new SellerSummaryDto(
                f.Service.Seller.Id,
                f.Service.Seller.Username,
                f.Service.Seller.FirstName,
                f.Service.Seller.PhotoUrl,
                f.Service.Seller.IsVerified,
                f.Service.AverageRating)
        )).ToList();

        return Result.Success<IReadOnlyList<ServiceListDto>>(serviceDtos);
    }

    public async Task<Result> AddToFavoritesAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(serviceId, cancellationToken);
        if (service == null)
            return Result.Failure("Service not found", "NOT_FOUND");

        var exists = await _unitOfWork.Favorites.ExistsAsync(userId, serviceId, cancellationToken);
        if (exists)
            return Result.Failure("Service is already in favorites", "ALREADY_EXISTS");

        var favorite = Favorite.Create(userId, serviceId);
        await _unitOfWork.Favorites.AddAsync(favorite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RemoveFromFavoritesAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        var favorites = await _unitOfWork.Favorites.FindAsync(f => f.UserId == userId && f.ServiceId == serviceId, cancellationToken);
        var favorite = favorites.FirstOrDefault();

        if (favorite == null)
            return Result.Failure("Favorite not found", "NOT_FOUND");

        _unitOfWork.Favorites.Remove(favorite);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<bool>> IsFavoriteAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        var exists = await _unitOfWork.Favorites.ExistsAsync(userId, serviceId, cancellationToken);
        return Result.Success(exists);
    }
}
