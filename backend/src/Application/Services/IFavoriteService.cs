using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;

namespace TelegramMarketplace.Application.Services;

public interface IFavoriteService
{
    Task<Result<IReadOnlyList<ServiceListDto>>> GetUserFavoritesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> AddToFavoritesAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default);
    Task<Result> RemoveFromFavoritesAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsFavoriteAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default);
}
