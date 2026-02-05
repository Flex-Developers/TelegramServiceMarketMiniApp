using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;

namespace TelegramMarketplace.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceService _serviceService;
    private readonly IFavoriteService _favoriteService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceService serviceService,
        IFavoriteService favoriteService,
        ILogger<ServicesController> logger)
    {
        _serviceService = serviceService;
        _favoriteService = favoriteService;
        _logger = logger;
    }

    private Guid? GetUserId() =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    /// <summary>
    /// Get paginated list of services with filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ServiceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices([FromQuery] ServiceFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _serviceService.GetPagedAsync(filter, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get featured services
    /// </summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedServices([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        var result = await _serviceService.GetFeaturedAsync(count, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get service by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetService(Guid id, CancellationToken cancellationToken)
    {
        var result = await _serviceService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        // Increment view count (fire and forget)
        _ = _serviceService.IncrementViewCountAsync(id, CancellationToken.None);

        // Check if favorited by current user
        var userId = GetUserId();
        bool? isFavorite = null;
        if (userId.HasValue)
        {
            var favoriteResult = await _favoriteService.IsFavoriteAsync(userId.Value, id, cancellationToken);
            if (favoriteResult.IsSuccess)
                isFavorite = favoriteResult.Value;
        }

        return Ok(new { service = result.Value, isFavorite });
    }

    /// <summary>
    /// Get services by seller ID
    /// </summary>
    [HttpGet("seller/{sellerId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSellerServices(Guid sellerId, CancellationToken cancellationToken)
    {
        var result = await _serviceService.GetBySellerIdAsync(sellerId, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new service (seller only)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (!sellerId.HasValue)
            return Unauthorized();

        var result = await _serviceService.CreateAsync(sellerId.Value, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return CreatedAtAction(nameof(GetService), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update a service (seller only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (!sellerId.HasValue)
            return Unauthorized();

        var result = await _serviceService.UpdateAsync(id, sellerId.Value, request, cancellationToken);
        if (result.IsFailure)
        {
            return result.ErrorCode switch
            {
                "FORBIDDEN" => Forbid(),
                "NOT_FOUND" => NotFound(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error, code = result.ErrorCode })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a service (seller only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteService(Guid id, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (!sellerId.HasValue)
            return Unauthorized();

        var result = await _serviceService.DeleteAsync(id, sellerId.Value, cancellationToken);
        if (result.IsFailure)
        {
            return result.ErrorCode switch
            {
                "FORBIDDEN" => Forbid(),
                "NOT_FOUND" => NotFound(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Activate a service
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivateService(Guid id, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (!sellerId.HasValue)
            return Unauthorized();

        var result = await _serviceService.ActivateAsync(id, sellerId.Value, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Service activated" });
    }

    /// <summary>
    /// Deactivate a service
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateService(Guid id, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (!sellerId.HasValue)
            return Unauthorized();

        var result = await _serviceService.DeactivateAsync(id, sellerId.Value, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Service deactivated" });
    }
}
