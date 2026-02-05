using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;

namespace TelegramMarketplace.Application.Services;

public interface IAuthService
{
    Task<Result<AuthResultDto>> AuthenticateWithTelegramAsync(string initData, CancellationToken cancellationToken = default);
    Task<Result<AuthResultDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    TelegramInitData? ParseInitData(string initData);
    bool ValidateInitData(string initData, string botToken);
}
