using Microsoft.EntityFrameworkCore;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Infrastructure.Persistence.Repositories;

public class ServiceRepository : BaseRepository<Service>, IServiceRepository
{
    public ServiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Service?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Seller)
            .Include(s => s.Category)
            .Include(s => s.Images.OrderBy(i => i.SortOrder))
            .Include(s => s.Tags)
                .ThenInclude(st => st.Tag)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(s => s.Seller)
            .Include(s => s.Images.OrderBy(i => i.SortOrder).Take(1))
            .Where(s => s.IsActive)
            .AsQueryable();

        // Apply filters
        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(s => s.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(s => s.Price <= maxPrice.Value);

        if (minRating.HasValue)
            query = query.Where(s => s.AverageRating >= minRating.Value);

        if (maxDeliveryDays.HasValue)
            query = query.Where(s => s.DeliveryDays <= maxDeliveryDays.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(s =>
                s.Title.ToLower().Contains(term) ||
                s.Description.ToLower().Contains(term));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting (cast decimal to double for SQLite compatibility)
        query = sortBy?.ToLower() switch
        {
            "price" => sortDescending ? query.OrderByDescending(s => (double)s.Price) : query.OrderBy(s => (double)s.Price),
            "rating" => sortDescending ? query.OrderByDescending(s => (double)s.AverageRating) : query.OrderBy(s => (double)s.AverageRating),
            "delivery" => sortDescending ? query.OrderByDescending(s => s.DeliveryDays) : query.OrderBy(s => s.DeliveryDays),
            "orders" => sortDescending ? query.OrderByDescending(s => s.OrderCount) : query.OrderBy(s => s.OrderCount),
            "newest" => query.OrderByDescending(s => s.CreatedAt),
            _ => query.OrderByDescending(s => s.OrderCount).ThenByDescending(s => (double)s.AverageRating)
        };

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Service>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Seller)
            .Include(s => s.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(s => s.Category)
            .Where(s => s.SellerId == sellerId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Seller)
            .Include(s => s.Images.OrderBy(i => i.SortOrder).Take(1))
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.OrderCount)
            .ThenByDescending(s => (double)s.AverageRating)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
