using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;

namespace TelegramMarketplace.Application.Services;

public interface ICartService
{
    Task<Result<CartDto>> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<CartItemDto>> AddItemAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default);
    Task<Result<CartItemDto>> UpdateItemQuantityAsync(Guid userId, Guid itemId, int quantity, CancellationToken cancellationToken = default);
    Task<Result> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<Result> ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<PromoCodeResultDto>> ApplyPromoCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default);
}
