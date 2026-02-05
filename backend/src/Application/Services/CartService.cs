using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Application.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;

    public CartService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartDto>> GetCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cartItems = await _unitOfWork.CartItems.GetByUserIdAsync(userId, cancellationToken);

        var items = cartItems.Select(item => {
            var firstImage = item.Service.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
            return new CartItemDto(
                item.Id,
                item.ServiceId,
                item.Service.Title,
                item.Service.Price,
                firstImage?.ThumbnailUrl ?? firstImage?.ImageUrl,
                item.Quantity,
                item.Quantity * item.Service.Price,
                new SellerSummaryDto(
                item.Service.Seller.Id,
                item.Service.Seller.Username,
                item.Service.Seller.FirstName,
                item.Service.Seller.PhotoUrl,
                item.Service.Seller.IsVerified,
                item.Service.AverageRating));
        }).ToList();

        var subTotal = items.Sum(i => i.TotalPrice);

        return Result.Success(new CartDto(
            items,
            subTotal,
            null,
            null,
            subTotal,
            items.Sum(i => i.Quantity)));
    }

    public async Task<Result<CartItemDto>> AddItemAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetWithDetailsAsync(request.ServiceId, cancellationToken);
        if (service == null)
            return Result.Failure<CartItemDto>("Service not found", "NOT_FOUND");

        if (!service.IsActive)
            return Result.Failure<CartItemDto>("Service is not available", "SERVICE_UNAVAILABLE");

        if (service.SellerId == userId)
            return Result.Failure<CartItemDto>("You cannot add your own service to cart", "INVALID_OPERATION");

        var existingItem = await _unitOfWork.CartItems.GetByUserAndServiceAsync(userId, request.ServiceId, cancellationToken);

        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + request.Quantity);
            _unitOfWork.CartItems.Update(existingItem);
        }
        else
        {
            existingItem = CartItem.Create(userId, request.ServiceId, request.Quantity);
            await _unitOfWork.CartItems.AddAsync(existingItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var firstImg = service.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
        return Result.Success(new CartItemDto(
            existingItem.Id,
            service.Id,
            service.Title,
            service.Price,
            firstImg?.ThumbnailUrl ?? firstImg?.ImageUrl,
            existingItem.Quantity,
            existingItem.Quantity * service.Price,
            new SellerSummaryDto(
                service.Seller.Id,
                service.Seller.Username,
                service.Seller.FirstName,
                service.Seller.PhotoUrl,
                service.Seller.IsVerified,
                service.AverageRating)));
    }

    public async Task<Result<CartItemDto>> UpdateItemQuantityAsync(Guid userId, Guid itemId, int quantity, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.CartItems.GetByIdAsync(itemId, cancellationToken);
        if (item == null)
            return Result.Failure<CartItemDto>("Cart item not found", "NOT_FOUND");

        if (item.UserId != userId)
            return Result.Failure<CartItemDto>("Access denied", "FORBIDDEN");

        if (quantity <= 0)
        {
            _unitOfWork.CartItems.Remove(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CartItemDto>("Item removed from cart", "ITEM_REMOVED");
        }

        item.UpdateQuantity(quantity);
        _unitOfWork.CartItems.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var service = await _unitOfWork.Services.GetWithDetailsAsync(item.ServiceId, cancellationToken);
        var serviceImg = service!.Images.OrderBy(i => i.SortOrder).FirstOrDefault();

        return Result.Success(new CartItemDto(
            item.Id,
            item.ServiceId,
            service.Title,
            service.Price,
            serviceImg?.ThumbnailUrl ?? serviceImg?.ImageUrl,
            item.Quantity,
            item.Quantity * service.Price,
            new SellerSummaryDto(
                service.Seller.Id,
                service.Seller.Username,
                service.Seller.FirstName,
                service.Seller.PhotoUrl,
                service.Seller.IsVerified,
                service.AverageRating)));
    }

    public async Task<Result> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.CartItems.GetByIdAsync(itemId, cancellationToken);
        if (item == null)
            return Result.Failure("Cart item not found", "NOT_FOUND");

        if (item.UserId != userId)
            return Result.Failure("Access denied", "FORBIDDEN");

        _unitOfWork.CartItems.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.CartItems.ClearUserCartAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PromoCodeResultDto>> ApplyPromoCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.PromoCodes.GetByCodeAsync(code.ToUpperInvariant(), cancellationToken);

        if (promoCode == null)
            return Result.Success(new PromoCodeResultDto(false, "Промокод не найден", null, null));

        if (!promoCode.IsValid())
            return Result.Success(new PromoCodeResultDto(false, "Промокод недействителен или истёк", null, null));

        var cart = await GetCartAsync(userId, cancellationToken);
        if (cart.IsFailure)
            return Result.Failure<PromoCodeResultDto>(cart.Error!, cart.ErrorCode);

        var discount = promoCode.CalculateDiscount(cart.Value!.SubTotal);
        var newTotal = cart.Value.SubTotal - discount;

        return Result.Success(new PromoCodeResultDto(
            true,
            $"Промокод применён! Скидка: {discount:N0} ₽",
            discount,
            newTotal));
    }
}
