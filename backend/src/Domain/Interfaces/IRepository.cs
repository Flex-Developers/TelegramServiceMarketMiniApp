using System.Linq.Expressions;
using TelegramMarketplace.Domain.Common;

namespace TelegramMarketplace.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}

public interface IUserRepository : IRepository<Entities.User>
{
    Task<Entities.User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Entities.User?> GetWithOrdersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.User?> GetWithServicesAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IServiceRepository : IRepository<Entities.Service>
{
    Task<Entities.Service?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Entities.Service> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minRating = null,
        int? maxDeliveryDays = null,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Service>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Service>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);
}

public interface IOrderRepository : IRepository<Entities.Order>
{
    Task<Entities.Order?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Entities.Order> Items, int TotalCount)> GetByBuyerIdPagedAsync(
        Guid buyerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Entities.Order> Items, int TotalCount)> GetBySellerIdPagedAsync(
        Guid sellerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<decimal> GetSellerRevenueAsync(Guid sellerId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository : IRepository<Entities.Category>
{
    Task<IReadOnlyList<Entities.Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<Entities.Category?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICartItemRepository : IRepository<Entities.CartItem>
{
    Task<IReadOnlyList<Entities.CartItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Entities.CartItem?> GetByUserAndServiceAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default);
    Task ClearUserCartAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IFavoriteRepository : IRepository<Entities.Favorite>
{
    Task<IReadOnlyList<Entities.Favorite>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default);
}

public interface IReviewRepository : IRepository<Entities.Review>
{
    Task<(IReadOnlyList<Entities.Review> Items, int TotalCount)> GetByServiceIdPagedAsync(
        Guid serviceId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(decimal AverageRating, int TotalCount)> GetServiceRatingStatsAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<bool> HasUserReviewedOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default);
}

public interface IPaymentRepository : IRepository<Entities.Payment>
{
    Task<Entities.Payment?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<Entities.Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public interface IPromoCodeRepository : IRepository<Entities.PromoCode>
{
    Task<Entities.PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}

public interface INotificationRepository : IRepository<Entities.Notification>
{
    Task<IReadOnlyList<Entities.Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Entities.Notification> Items, int TotalCount)> GetByUserIdPagedAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
