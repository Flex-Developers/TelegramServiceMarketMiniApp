using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;

namespace TelegramMarketplace.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate using Telegram Mini App initData
    /// </summary>
    [HttpPost("telegram")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AuthenticateWithTelegram([FromBody] TelegramAuthRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.AuthenticateWithTelegramAsync(request.InitData, cancellationToken);

        if (result.IsFailure || result.Value?.Success != true)
        {
            _logger.LogWarning("Telegram authentication failed: {Error}", result.Value?.Error ?? result.Error);
            return Unauthorized(new { error = result.Value?.Error ?? result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (result.IsFailure || result.Value?.Success != true)
        {
            return Unauthorized(new { error = result.Value?.Error ?? result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Revoke refresh token (logout)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var firstName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var telegramId = User.FindFirst("telegram_id")?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isVerified = User.FindFirst("is_verified")?.Value;

        return Ok(new
        {
            UserId = userId,
            FirstName = firstName,
            TelegramId = telegramId,
            Role = role,
            IsVerified = isVerified
        });
    }
}
