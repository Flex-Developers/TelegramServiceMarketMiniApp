using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Application.Services;

public interface IOrderService
{
    Task<Result<OrderDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<OrderListDto>>> GetBuyerOrdersAsync(Guid buyerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<OrderListDto>>> GetSellerOrdersAsync(Guid sellerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> CreateFromCartAsync(Guid buyerId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> UpdateStatusAsync(Guid orderId, Guid sellerId, OrderStatus newStatus, CancellationToken cancellationToken = default);
    Task<Result> CancelOrderAsync(Guid orderId, Guid userId, string reason, CancellationToken cancellationToken = default);
    Task<Result> MarkAsPaidAsync(Guid orderId, CancellationToken cancellationToken = default);
}
