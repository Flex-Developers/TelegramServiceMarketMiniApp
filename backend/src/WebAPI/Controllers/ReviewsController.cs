using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;

namespace TelegramMarketplace.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private Guid? GetUserId() =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    /// <summary>
    /// Get reviews for a service
    /// </summary>
    [HttpGet("service/{serviceId:guid}")]
    [ProducesResponseType(typeof(PagedResult<ReviewListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServiceReviews(Guid serviceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _reviewService.GetByServiceIdAsync(serviceId, page, pageSize, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get review statistics for a service
    /// </summary>
    [HttpGet("service/{serviceId:guid}/stats")]
    [ProducesResponseType(typeof(ReviewStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServiceReviewStats(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetServiceReviewStatsAsync(serviceId, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a review for completed order
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _reviewService.CreateAsync(userId.Value, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return CreatedAtAction(nameof(GetServiceReviews), new { serviceId = result.Value!.ServiceId }, result.Value);
    }

    /// <summary>
    /// Update a review
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _reviewService.UpdateAsync(id, userId.Value, request, cancellationToken);
        if (result.IsFailure)
        {
            return result.ErrorCode switch
            {
                "FORBIDDEN" => Forbid(),
                "NOT_FOUND" => NotFound(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Add seller response to review
    /// </summary>
    [HttpPost("{id:guid}/response")]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddSellerResponse(Guid id, [FromBody] SellerResponseRequest request, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (!sellerId.HasValue)
            return Unauthorized();

        var result = await _reviewService.AddSellerResponseAsync(id, sellerId.Value, request.Response, cancellationToken);
        if (result.IsFailure)
        {
            return result.ErrorCode switch
            {
                "FORBIDDEN" => Forbid(),
                "NOT_FOUND" => NotFound(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Vote review as helpful
    /// </summary>
    [HttpPost("{id:guid}/helpful")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VoteHelpful(Guid id, CancellationToken cancellationToken)
    {
        var result = await _reviewService.VoteHelpfulAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(new { message = "Vote recorded" });
    }
}
