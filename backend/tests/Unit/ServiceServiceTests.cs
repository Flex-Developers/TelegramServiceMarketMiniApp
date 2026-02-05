using FluentAssertions;
using Moq;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Enums;
using TelegramMarketplace.Domain.Interfaces;
using Xunit;

namespace TelegramMarketplace.Tests.Unit;

public class ServiceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ServiceService _serviceService;

    public ServiceServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _serviceService = new ServiceService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenServiceExists_ReturnsSuccess()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var seller = User.Create(123456789, "Test", "User", "testuser");
        var category = Category.Create("Test Category", "Test Category EN", "Test Category DE");
        var service = Service.Create(sellerId, "Test Service", "Description", categoryId, 1000, PriceType.Fixed, 3);

        // Use reflection to set navigation properties for testing
        typeof(Service).GetProperty("Seller")!.SetValue(service, seller);
        typeof(Service).GetProperty("Category")!.SetValue(service, category);

        _unitOfWorkMock.Setup(x => x.Services.GetWithDetailsAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        // Act
        var result = await _serviceService.GetByIdAsync(serviceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Service");
    }

    [Fact]
    public async Task GetByIdAsync_WhenServiceNotExists_ReturnsFailure()
    {
        // Arrange
        var serviceId = Guid.NewGuid();

        _unitOfWorkMock.Setup(x => x.Services.GetWithDetailsAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        // Act
        var result = await _serviceService.GetByIdAsync(serviceId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesService()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var seller = User.Create(123456789, "Seller", null, "seller");
        var category = Category.Create("Category", "Category EN", "Category DE");

        var request = new CreateServiceRequest(
            "New Service",
            "Service Description",
            categoryId,
            5000,
            PriceType.Fixed,
            7,
            24,
            new List<string> { "https://example.com/image.jpg" },
            new List<string> { "tag1", "tag2" }
        );

        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seller);
        _unitOfWorkMock.Setup(x => x.Categories.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _unitOfWorkMock.Setup(x => x.Services.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service s, CancellationToken _) => s);
        _unitOfWorkMock.Setup(x => x.Services.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                var s = Service.Create(sellerId, request.Title, request.Description, categoryId, request.Price, request.PriceType, request.DeliveryDays);
                typeof(Service).GetProperty("Seller")!.SetValue(s, seller);
                typeof(Service).GetProperty("Category")!.SetValue(s, category);
                return s;
            });
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _serviceService.CreateAsync(sellerId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Service");
        result.Value.Price.Should().Be(5000);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCategory_ReturnsFailure()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var seller = User.Create(123456789, "Seller", null, "seller");

        var request = new CreateServiceRequest(
            "New Service",
            "Description",
            categoryId,
            1000,
            PriceType.Fixed,
            3,
            24,
            new List<string>(),
            null
        );

        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seller);
        _unitOfWorkMock.Setup(x => x.Categories.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _serviceService.CreateAsync(sellerId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_CATEGORY");
    }
}
