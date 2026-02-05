namespace TelegramMarketplace.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IServiceRepository Services { get; }
    IOrderRepository Orders { get; }
    ICategoryRepository Categories { get; }
    ICartItemRepository CartItems { get; }
    IFavoriteRepository Favorites { get; }
    IReviewRepository Reviews { get; }
    IPaymentRepository Payments { get; }
    IPromoCodeRepository PromoCodes { get; }
    INotificationRepository Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    void DetachEntity<T>(T entity) where T : class;
    Task DeleteServiceImagesAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task AddServiceImageAsync(Entities.ServiceImage image, CancellationToken cancellationToken = default);
}
