using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ReviewService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<Result<ReviewDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id, cancellationToken);
        if (review == null)
            return Result.Failure<ReviewDto>("Review not found", "NOT_FOUND");

        return Result.Success(MapToDto(review));
    }

    public async Task<Result<PagedResult<ReviewListDto>>> GetByServiceIdAsync(Guid serviceId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (reviews, totalCount) = await _unitOfWork.Reviews.GetByServiceIdPagedAsync(serviceId, page, pageSize, cancellationToken);
        var dtos = reviews.Select(MapToListDto).ToList();
        return Result.Success(new PagedResult<ReviewListDto>(dtos, totalCount, page, pageSize));
    }

    public async Task<Result<ReviewStatsDto>> GetServiceReviewStatsAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var (reviews, _) = await _unitOfWork.Reviews.GetByServiceIdPagedAsync(serviceId, 1, 1000, cancellationToken);

        if (!reviews.Any())
        {
            return Result.Success(new ReviewStatsDto(0, 0, 0, 0, 0, 0, 0));
        }

        var average = reviews.Average(r => r.Rating);
        var total = reviews.Count;

        return Result.Success(new ReviewStatsDto(
            (decimal)average,
            total,
            reviews.Count(r => r.Rating == 5),
            reviews.Count(r => r.Rating == 4),
            reviews.Count(r => r.Rating == 3),
            reviews.Count(r => r.Rating == 2),
            reviews.Count(r => r.Rating == 1)));
    }

    public async Task<Result<ReviewDto>> CreateAsync(Guid reviewerId, CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(request.OrderId, cancellationToken);
        if (order == null)
            return Result.Failure<ReviewDto>("Order not found", "NOT_FOUND");

        if (order.BuyerId != reviewerId)
            return Result.Failure<ReviewDto>("You can only review orders you've purchased", "FORBIDDEN");

        if (order.Status != Domain.Enums.OrderStatus.Completed)
            return Result.Failure<ReviewDto>("You can only review completed orders", "ORDER_NOT_COMPLETED");

        var hasReviewed = await _unitOfWork.Reviews.HasUserReviewedOrderAsync(reviewerId, request.OrderId, cancellationToken);
        if (hasReviewed)
            return Result.Failure<ReviewDto>("You have already reviewed this order", "ALREADY_REVIEWED");

        var serviceId = order.Items.First().ServiceId;
        var review = Review.Create(
            request.OrderId,
            serviceId,
            reviewerId,
            order.SellerId,
            request.Rating,
            request.Comment,
            request.Images);

        await _unitOfWork.Reviews.AddAsync(review, cancellationToken);

        // Update service rating
        var service = await _unitOfWork.Services.GetByIdAsync(serviceId, cancellationToken);
        if (service != null)
        {
            var (avgRating, reviewCount) = await _unitOfWork.Reviews.GetServiceRatingStatsAsync(serviceId, cancellationToken);
            service.UpdateRating(avgRating, reviewCount + 1);
            _unitOfWork.Services.Update(service);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify seller
        var reviewer = await _unitOfWork.Users.GetByIdAsync(reviewerId, cancellationToken);
        await _notificationService.SendReviewNotificationAsync(order.SellerId, reviewer!.FirstName, request.Rating, cancellationToken);

        var createdReview = await _unitOfWork.Reviews.GetByIdAsync(review.Id, cancellationToken);
        return Result.Success(MapToDto(createdReview!));
    }

    public async Task<Result<ReviewDto>> UpdateAsync(Guid reviewId, Guid reviewerId, UpdateReviewRequest request, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken);
        if (review == null)
            return Result.Failure<ReviewDto>("Review not found", "NOT_FOUND");

        if (review.ReviewerId != reviewerId)
            return Result.Failure<ReviewDto>("You can only edit your own reviews", "FORBIDDEN");

        review.Update(request.Rating, request.Comment, request.Images);
        _unitOfWork.Reviews.Update(review);

        // Update service rating
        var (avgRating, reviewCount) = await _unitOfWork.Reviews.GetServiceRatingStatsAsync(review.ServiceId, cancellationToken);
        var service = await _unitOfWork.Services.GetByIdAsync(review.ServiceId, cancellationToken);
        if (service != null)
        {
            service.UpdateRating(avgRating, reviewCount);
            _unitOfWork.Services.Update(service);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(review));
    }

    public async Task<Result<ReviewDto>> AddSellerResponseAsync(Guid reviewId, Guid sellerId, string response, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken);
        if (review == null)
            return Result.Failure<ReviewDto>("Review not found", "NOT_FOUND");

        if (review.SellerId != sellerId)
            return Result.Failure<ReviewDto>("You can only respond to reviews of your services", "FORBIDDEN");

        review.AddSellerResponse(response);
        _unitOfWork.Reviews.Update(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(review));
    }

    public async Task<Result> VoteHelpfulAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken);
        if (review == null)
            return Result.Failure("Review not found", "NOT_FOUND");

        review.IncrementHelpfulVotes();
        _unitOfWork.Reviews.Update(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private ReviewDto MapToDto(Review review)
    {
        return new ReviewDto(
            review.Id,
            review.OrderId,
            review.ServiceId,
            review.ReviewerId,
            new UserSummaryDto(review.Reviewer.Id, review.Reviewer.Username, review.Reviewer.FirstName, review.Reviewer.PhotoUrl),
            review.Rating,
            review.Comment,
            review.Images,
            review.SellerResponse,
            review.ResponseDate,
            review.HelpfulVotes,
            review.IsVerifiedPurchase,
            review.CreatedAt,
            review.UpdatedAt);
    }

    private ReviewListDto MapToListDto(Review review)
    {
        return new ReviewListDto(
            review.Id,
            new UserSummaryDto(review.Reviewer.Id, review.Reviewer.Username, review.Reviewer.FirstName, review.Reviewer.PhotoUrl),
            review.Rating,
            review.Comment,
            review.Images,
            review.SellerResponse,
            review.HelpfulVotes,
            review.IsVerifiedPurchase,
            review.CreatedAt);
    }
}
