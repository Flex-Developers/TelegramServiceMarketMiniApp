using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;

namespace TelegramMarketplace.Application.Services;

public interface IServiceService
{
    Task<Result<ServiceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ServiceListDto>>> GetPagedAsync(ServiceFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ServiceListDto>>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ServiceListDto>>> GetFeaturedAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<Result<ServiceDto>> CreateAsync(Guid sellerId, CreateServiceRequest request, CancellationToken cancellationToken = default);
    Task<Result<ServiceDto>> UpdateAsync(Guid id, Guid sellerId, UpdateServiceRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default);
    Task<Result> ActivateAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default);
    Task<Result> IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default);
}
