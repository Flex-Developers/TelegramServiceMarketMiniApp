using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TelegramMarketplace.Domain.Interfaces;
using TelegramMarketplace.Infrastructure.Persistence.Repositories;

namespace TelegramMarketplace.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _users;
    private IServiceRepository? _services;
    private IOrderRepository? _orders;
    private ICategoryRepository? _categories;
    private ICartItemRepository? _cartItems;
    private IFavoriteRepository? _favorites;
    private IReviewRepository? _reviews;
    private IPaymentRepository? _payments;
    private IPromoCodeRepository? _promoCodes;
    private INotificationRepository? _notifications;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IServiceRepository Services => _services ??= new ServiceRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public ICartItemRepository CartItems => _cartItems ??= new CartItemRepository(_context);
    public IFavoriteRepository Favorites => _favorites ??= new FavoriteRepository(_context);
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public IPromoCodeRepository PromoCodes => _promoCodes ??= new PromoCodeRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void DetachEntity<T>(T entity) where T : class
    {
        _context.Entry(entity).State = EntityState.Detached;
    }

    public async Task DeleteServiceImagesAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var images = await _context.ServiceImages
            .Where(i => i.ServiceId == serviceId)
            .ToListAsync(cancellationToken);
        _context.ServiceImages.RemoveRange(images);
    }

    public async Task AddServiceImageAsync(TelegramMarketplace.Domain.Entities.ServiceImage image, CancellationToken cancellationToken = default)
    {
        await _context.ServiceImages.AddAsync(image, cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
