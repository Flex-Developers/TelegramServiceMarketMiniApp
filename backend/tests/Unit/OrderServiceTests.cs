using FluentAssertions;
using Moq;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Enums;
using TelegramMarketplace.Domain.Interfaces;
using Xunit;

namespace TelegramMarketplace.Tests.Unit;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notificationServiceMock = new Mock<INotificationService>();
        _orderService = new OrderService(_unitOfWorkMock.Object, _notificationServiceMock.Object, 10);
    }

    [Fact]
    public async Task MarkAsPaidAsync_WhenOrderExists_UpdatesStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var order = Order.Create(buyerId, sellerId, 1000, 10, PaymentMethod.YooKassa);

        _unitOfWorkMock.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _orderService.MarkAsPaidAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.PaymentStatus.Should().Be(PaymentStatus.Completed);
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task MarkAsPaidAsync_WhenOrderNotExists_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _unitOfWorkMock.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.MarkAsPaidAsync(orderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task CancelOrderAsync_WhenAuthorized_CancelsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var reason = "Changed my mind";

        var buyer = User.Create(123, "Buyer", null, "buyer");
        var seller = User.Create(456, "Seller", null, "seller");
        var order = Order.Create(buyerId, sellerId, 1000, 10, PaymentMethod.YooKassa);

        typeof(Order).GetProperty("Buyer")!.SetValue(order, buyer);
        typeof(Order).GetProperty("Seller")!.SetValue(order, seller);

        _unitOfWorkMock.Setup(x => x.Orders.GetWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _orderService.CancelOrderAsync(orderId, buyerId, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be(reason);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenNotAuthorized_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var reason = "Test";

        var buyer = User.Create(123, "Buyer", null, "buyer");
        var seller = User.Create(456, "Seller", null, "seller");
        var order = Order.Create(buyerId, sellerId, 1000, 10, PaymentMethod.YooKassa);

        typeof(Order).GetProperty("Buyer")!.SetValue(order, buyer);
        typeof(Order).GetProperty("Seller")!.SetValue(order, seller);

        _unitOfWorkMock.Setup(x => x.Orders.GetWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.CancelOrderAsync(orderId, unauthorizedUserId, reason);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }
}
