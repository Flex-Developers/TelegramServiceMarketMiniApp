using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramMarketplace.Domain.Interfaces;

namespace TelegramMarketplace.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUnitOfWork unitOfWork, ILogger<UsersController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Become a seller (change role from Buyer to Both)
    /// </summary>
    [HttpPost("become-seller")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BecomeSeller(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (user.Role == Domain.Enums.UserRole.Seller ||
            user.Role == Domain.Enums.UserRole.Both ||
            user.Role == Domain.Enums.UserRole.Admin)
        {
            return BadRequest(new { error = "User is already a seller" });
        }

        user.BecomeSeller();
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} became a seller", userId);

        return Ok(new {
            message = "Successfully became a seller",
            role = user.Role.ToString()
        });
    }
}
