using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;

namespace TelegramMarketplace.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Get user's favorite services
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFavorites(CancellationToken cancellationToken)
    {
        var result = await _favoriteService.GetUserFavoritesAsync(GetUserId(), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Add service to favorites
    /// </summary>
    [HttpPost("{serviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddToFavorites(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await _favoriteService.AddToFavoritesAsync(GetUserId(), serviceId, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Created($"/api/favorites", new { message = "Added to favorites" });
    }

    /// <summary>
    /// Remove service from favorites
    /// </summary>
    [HttpDelete("{serviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromFavorites(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await _favoriteService.RemoveFromFavoritesAsync(GetUserId(), serviceId, cancellationToken);
        if (result.IsFailure)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Check if service is in favorites
    /// </summary>
    [HttpGet("{serviceId:guid}/check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckFavorite(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await _favoriteService.IsFavoriteAsync(GetUserId(), serviceId, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { isFavorite = result.Value });
    }
}
