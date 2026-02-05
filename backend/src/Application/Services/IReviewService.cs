using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;

namespace TelegramMarketplace.Application.Services;

public interface IReviewService
{
    Task<Result<ReviewDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ReviewListDto>>> GetByServiceIdAsync(Guid serviceId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<ReviewStatsDto>> GetServiceReviewStatsAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<Result<ReviewDto>> CreateAsync(Guid reviewerId, CreateReviewRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReviewDto>> UpdateAsync(Guid reviewId, Guid reviewerId, UpdateReviewRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReviewDto>> AddSellerResponseAsync(Guid reviewId, Guid sellerId, string response, CancellationToken cancellationToken = default);
    Task<Result> VoteHelpfulAsync(Guid reviewId, CancellationToken cancellationToken = default);
}
