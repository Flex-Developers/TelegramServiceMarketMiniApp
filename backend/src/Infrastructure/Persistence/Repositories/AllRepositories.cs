using Microsoft.EntityFrameworkCore;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken);
    }

    public async Task<User?> GetWithOrdersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.BuyerOrders)
            .Include(u => u.SellerOrders)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetWithServicesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Services)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}

public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Order?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Items)
                .ThenInclude(i => i.Service)
                    .ThenInclude(s => s.Images)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetByBuyerIdPagedAsync(
        Guid buyerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(o => o.Seller)
            .Include(o => o.Items)
                .ThenInclude(i => i.Service)
                    .ThenInclude(s => s.Images.OrderBy(img => img.SortOrder).Take(1))
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetBySellerIdPagedAsync(
        Guid sellerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(o => o.Buyer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Service)
                    .ThenInclude(s => s.Images.OrderBy(img => img.SortOrder).Take(1))
            .Where(o => o.SellerId == sellerId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<decimal> GetSellerRevenueAsync(Guid sellerId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(o => o.SellerId == sellerId && o.PaymentStatus == Domain.Enums.PaymentStatus.Completed);

        if (from.HasValue)
            query = query.Where(o => o.CompletedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(o => o.CompletedAt <= to.Value);

        return await query.SumAsync(o => o.TotalAmount - o.Commission, cancellationToken);
    }
}

public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Services)
            .Where(c => c.ParentId == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Services)
            .Where(c => c.ParentId == parentId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Children)
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}

public class CartItemRepository : BaseRepository<CartItem>, ICartItemRepository
{
    public CartItemRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Service)
                .ThenInclude(s => s.Seller)
            .Include(c => c.Service)
                .ThenInclude(s => s.Images.OrderBy(i => i.SortOrder).Take(1))
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CartItem?> GetByUserAndServiceAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.UserId == userId && c.ServiceId == serviceId, cancellationToken);
    }

    public async Task ClearUserCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await _dbSet.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
        _dbSet.RemoveRange(items);
    }
}

public class FavoriteRepository : BaseRepository<Favorite>, IFavoriteRepository
{
    public FavoriteRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Favorite>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(f => f.Service)
                .ThenInclude(s => s.Seller)
            .Include(f => f.Service)
                .ThenInclude(s => s.Images.OrderBy(i => i.SortOrder).Take(1))
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(f => f.UserId == userId && f.ServiceId == serviceId, cancellationToken);
    }
}

public class ReviewRepository : BaseRepository<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByServiceIdPagedAsync(
        Guid serviceId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Reviewer)
            .Where(r => r.ServiceId == serviceId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(decimal AverageRating, int TotalCount)> GetServiceRatingStatsAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var reviews = await _dbSet.Where(r => r.ServiceId == serviceId).ToListAsync(cancellationToken);
        if (!reviews.Any())
            return (0, 0);

        return ((decimal)reviews.Average(r => r.Rating), reviews.Count);
    }

    public async Task<bool> HasUserReviewedOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(r => r.ReviewerId == userId && r.OrderId == orderId, cancellationToken);
    }
}

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Payment?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }
}

public class PromoCodeRepository : BaseRepository<PromoCode>, IPromoCodeRepository
{
    public PromoCodeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Code == code.ToUpperInvariant(), cancellationToken);
    }
}

public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserIdPagedAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _dbSet.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in unread)
        {
            notification.MarkAsRead();
        }
    }
}
