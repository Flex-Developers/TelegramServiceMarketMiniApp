using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Enums;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly decimal _commissionPercentage;

    public OrderService(IUnitOfWork unitOfWork, INotificationService notificationService, decimal commissionPercentage = 10)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _commissionPercentage = commissionPercentage;
    }

    public async Task<Result<OrderDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id, cancellationToken);
        if (order == null)
            return Result.Failure<OrderDto>("Order not found", "NOT_FOUND");

        if (order.BuyerId != userId && order.SellerId != userId)
            return Result.Failure<OrderDto>("Access denied", "FORBIDDEN");

        return Result.Success(MapToDto(order));
    }

    public async Task<Result<PagedResult<OrderListDto>>> GetBuyerOrdersAsync(Guid buyerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (orders, totalCount) = await _unitOfWork.Orders.GetByBuyerIdPagedAsync(buyerId, page, pageSize, cancellationToken);
        var dtos = orders.Select(o => MapToListDto(o, isBuyer: true)).ToList();
        return Result.Success(new PagedResult<OrderListDto>(dtos, totalCount, page, pageSize));
    }

    public async Task<Result<PagedResult<OrderListDto>>> GetSellerOrdersAsync(Guid sellerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (orders, totalCount) = await _unitOfWork.Orders.GetBySellerIdPagedAsync(sellerId, page, pageSize, cancellationToken);
        var dtos = orders.Select(o => MapToListDto(o, isBuyer: false)).ToList();
        return Result.Success(new PagedResult<OrderListDto>(dtos, totalCount, page, pageSize));
    }

    public async Task<Result<OrderDto>> CreateFromCartAsync(Guid buyerId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var cartItems = await _unitOfWork.CartItems.GetByUserIdAsync(buyerId, cancellationToken);
        if (!cartItems.Any())
            return Result.Failure<OrderDto>("Cart is empty", "EMPTY_CART");

        // Group items by seller
        var itemsBySeller = cartItems.GroupBy(c => c.Service.SellerId);

        decimal discountAmount = 0;
        if (!string.IsNullOrEmpty(request.PromoCode))
        {
            var promoCode = await _unitOfWork.PromoCodes.GetByCodeAsync(request.PromoCode, cancellationToken);
            if (promoCode != null && promoCode.IsValid())
            {
                var subTotal = cartItems.Sum(c => c.Quantity * c.Service.Price);
                discountAmount = promoCode.CalculateDiscount(subTotal);
                promoCode.IncrementUsage();
            }
        }

        var createdOrders = new List<Order>();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var sellerGroup in itemsBySeller)
            {
                var sellerId = sellerGroup.Key;
                var items = sellerGroup.ToList();
                var subTotal = items.Sum(i => i.Quantity * i.Service.Price);

                var order = Order.Create(
                    buyerId,
                    sellerId,
                    subTotal,
                    _commissionPercentage,
                    request.PaymentMethod,
                    request.PromoCode,
                    discountAmount > 0 ? discountAmount / itemsBySeller.Count() : 0,
                    request.Notes);

                foreach (var cartItem in items)
                {
                    var orderItem = OrderItem.Create(
                        order.Id,
                        cartItem.ServiceId,
                        cartItem.Service.Title,
                        cartItem.Service.Description,
                        cartItem.Quantity,
                        cartItem.Service.Price);
                    order.AddItem(orderItem);

                    cartItem.Service.IncrementOrderCount();
                }

                await _unitOfWork.Orders.AddAsync(order, cancellationToken);
                createdOrders.Add(order);

                // Notify seller
                await _notificationService.SendOrderNotificationAsync(
                    sellerId,
                    NotificationType.OrderCreated,
                    "Новый заказ",
                    $"Получен новый заказ на сумму {order.TotalAmount:N0} ₽",
                    order.Id.ToString(),
                    cancellationToken);
            }

            // Clear cart
            await _unitOfWork.CartItems.ClearUserCartAsync(buyerId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Return first order (or aggregate in real scenario)
            var firstOrder = await _unitOfWork.Orders.GetWithDetailsAsync(createdOrders.First().Id, cancellationToken);
            return Result.Success(MapToDto(firstOrder!));
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<OrderDto>> UpdateStatusAsync(Guid orderId, Guid sellerId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId, cancellationToken);
        if (order == null)
            return Result.Failure<OrderDto>("Order not found", "NOT_FOUND");

        if (order.SellerId != sellerId)
            return Result.Failure<OrderDto>("Access denied", "FORBIDDEN");

        switch (newStatus)
        {
            case OrderStatus.Processing:
                order.MarkAsProcessing();
                break;
            case OrderStatus.Delivered:
                order.MarkAsDelivered();
                break;
            case OrderStatus.Completed:
                order.Complete();
                break;
            default:
                return Result.Failure<OrderDto>("Invalid status transition", "INVALID_STATUS");
        }

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify buyer
        await _notificationService.SendOrderNotificationAsync(
            order.BuyerId,
            NotificationType.OrderProcessing,
            "Статус заказа обновлён",
            GetStatusMessage(newStatus),
            order.Id.ToString(),
            cancellationToken);

        return Result.Success(MapToDto(order));
    }

    public async Task<Result> CancelOrderAsync(Guid orderId, Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId, cancellationToken);
        if (order == null)
            return Result.Failure("Order not found", "NOT_FOUND");

        if (order.BuyerId != userId && order.SellerId != userId)
            return Result.Failure("Access denied", "FORBIDDEN");

        order.Cancel(reason);
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify other party
        var notifyUserId = userId == order.BuyerId ? order.SellerId : order.BuyerId;
        await _notificationService.SendOrderNotificationAsync(
            notifyUserId,
            NotificationType.OrderCancelled,
            "Заказ отменён",
            $"Заказ был отменён. Причина: {reason}",
            order.Id.ToString(),
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAsPaidAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            return Result.Failure("Order not found", "NOT_FOUND");

        order.MarkAsPaid();
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify seller
        await _notificationService.SendOrderNotificationAsync(
            order.SellerId,
            NotificationType.PaymentReceived,
            "Оплата получена",
            $"Заказ оплачен на сумму {order.TotalAmount:N0} ₽",
            order.Id.ToString(),
            cancellationToken);

        return Result.Success();
    }

    private string GetStatusMessage(OrderStatus status) => status switch
    {
        OrderStatus.Processing => "Продавец начал выполнение заказа",
        OrderStatus.Delivered => "Заказ доставлен и ожидает подтверждения",
        OrderStatus.Completed => "Заказ успешно завершён",
        _ => "Статус заказа изменён"
    };

    private OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.BuyerId,
            order.SellerId,
            new UserSummaryDto(order.Buyer.Id, order.Buyer.Username, order.Buyer.FirstName, order.Buyer.PhotoUrl),
            new UserSummaryDto(order.Seller.Id, order.Seller.Username, order.Seller.FirstName, order.Seller.PhotoUrl),
            order.Status,
            order.SubTotal,
            order.Commission,
            order.TotalAmount,
            order.PaymentMethod,
            order.PaymentStatus,
            order.PromoCode,
            order.DiscountAmount,
            order.Notes,
            order.Items.Select(i => new OrderItemDto(
                i.Id, i.ServiceId, i.ServiceTitle, i.ServiceDescription, i.Quantity, i.UnitPrice, i.TotalPrice,
                i.Service?.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.ThumbnailUrl)).ToList(),
            order.CreatedAt,
            order.PaidAt,
            order.CompletedAt,
            order.CancelledAt,
            order.CancellationReason);
    }

    private OrderListDto MapToListDto(Order order, bool isBuyer)
    {
        var otherParty = isBuyer ? order.Seller : order.Buyer;
        var firstItem = order.Items.FirstOrDefault();

        return new OrderListDto(
            order.Id,
            order.Status,
            order.TotalAmount,
            order.PaymentStatus,
            order.Items.Count,
            firstItem?.ServiceTitle ?? "",
            firstItem?.Service?.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ThumbnailUrl,
            new UserSummaryDto(otherParty.Id, otherParty.Username, otherParty.FirstName, otherParty.PhotoUrl),
            order.CreatedAt);
    }
}
