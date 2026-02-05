using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TelegramMarketplace.Application.Common;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.Domain.Entities;
using TelegramMarketplace.Domain.Interfaces;
using TelegramMarketplace.Infrastructure.Caching;
using TelegramMarketplace.Infrastructure.Configuration;

namespace TelegramMarketplace.Infrastructure.Authentication;

public class TelegramAuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TelegramSettings _telegramSettings;
    private readonly JwtSettings _jwtSettings;
    private readonly ITokenService _tokenService;

    public TelegramAuthService(
        IUnitOfWork unitOfWork,
        IOptions<TelegramSettings> telegramSettings,
        IOptions<JwtSettings> jwtSettings,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _telegramSettings = telegramSettings.Value;
        _jwtSettings = jwtSettings.Value;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResultDto>> AuthenticateWithTelegramAsync(string initData, CancellationToken cancellationToken = default)
    {
        if (!ValidateInitData(initData, _telegramSettings.BotToken))
        {
            return Result.Success(new AuthResultDto(false, null, null, null, null, "Invalid Telegram authentication data"));
        }

        var telegramData = ParseInitData(initData);
        if (telegramData == null)
        {
            return Result.Success(new AuthResultDto(false, null, null, null, null, "Failed to parse authentication data"));
        }

        // Check auth_date (not older than 1 hour)
        var authTime = DateTimeOffset.FromUnixTimeSeconds(telegramData.AuthDate).UtcDateTime;
        if (DateTime.UtcNow - authTime > TimeSpan.FromHours(1))
        {
            return Result.Success(new AuthResultDto(false, null, null, null, null, "Authentication data expired"));
        }

        // Find or create user (with retry for race condition)
        var user = await _unitOfWork.Users.GetByTelegramIdAsync(telegramData.UserId, cancellationToken);
        var isNewUser = user == null;

        if (isNewUser)
        {
            user = User.Create(
                telegramData.UserId,
                telegramData.FirstName,
                telegramData.LastName,
                telegramData.Username,
                telegramData.PhotoUrl,
                telegramData.LanguageCode);

            await _unitOfWork.Users.AddAsync(user, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true ||
                      ex.InnerException?.Message.Contains("duplicate key") == true)
            {
                // Race condition: another request created the user
                // Detach the failed entity and fetch the existing one
                _unitOfWork.DetachEntity(user);
                user = await _unitOfWork.Users.GetByTelegramIdAsync(telegramData.UserId, cancellationToken);
                if (user == null)
                    throw; // Re-throw if still not found (unexpected error)
                isNewUser = false;
            }
        }

        if (!isNewUser)
        {
            user!.UpdateProfile(
                telegramData.FirstName,
                telegramData.LastName,
                telegramData.Username,
                telegramData.PhotoUrl);

            if (telegramData.LanguageCode != null)
                user.SetLanguage(telegramData.LanguageCode);

            user.UpdateLastActive();
            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Generate tokens
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user!);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user!.Id, cancellationToken);

        var userDto = new UserDto(
            user.Id,
            user.TelegramId,
            user.Username,
            user.FirstName,
            user.LastName,
            user.PhotoUrl,
            user.Role,
            user.IsVerified,
            user.LanguageCode,
            user.CreatedAt,
            user.LastActiveAt);

        return Result.Success(new AuthResultDto(true, accessToken, refreshToken, expiresAt, userDto, null));
    }

    public async Task<Result<AuthResultDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var userId = await _tokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
        if (userId == null)
        {
            return Result.Success(new AuthResultDto(false, null, null, null, null, "Invalid or expired refresh token"));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            return Result.Success(new AuthResultDto(false, null, null, null, null, "User not found"));
        }

        // Revoke old token and generate new ones
        await _tokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
        var (newAccessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, cancellationToken);

        var userDto = new UserDto(
            user.Id,
            user.TelegramId,
            user.Username,
            user.FirstName,
            user.LastName,
            user.PhotoUrl,
            user.Role,
            user.IsVerified,
            user.LanguageCode,
            user.CreatedAt,
            user.LastActiveAt);

        return Result.Success(new AuthResultDto(true, newAccessToken, newRefreshToken, expiresAt, userDto, null));
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        await _tokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
        return Result.Success();
    }

    public TelegramInitData? ParseInitData(string initData)
    {
        try
        {
            var pairs = HttpUtility.ParseQueryString(initData);
            var userJson = pairs["user"];

            if (string.IsNullOrEmpty(userJson))
                return null;

            using var doc = JsonDocument.Parse(userJson);
            var root = doc.RootElement;

            return new TelegramInitData(
                root.GetProperty("id").GetInt64(),
                root.GetProperty("first_name").GetString() ?? "",
                root.TryGetProperty("last_name", out var ln) ? ln.GetString() : null,
                root.TryGetProperty("username", out var un) ? un.GetString() : null,
                root.TryGetProperty("photo_url", out var pu) ? pu.GetString() : null,
                root.TryGetProperty("language_code", out var lc) ? lc.GetString() : null,
                long.Parse(pairs["auth_date"] ?? "0"),
                pairs["hash"] ?? "");
        }
        catch
        {
            return null;
        }
    }

    public bool ValidateInitData(string initData, string botToken)
    {
        try
        {
            var pairs = HttpUtility.ParseQueryString(initData);
            var hash = pairs["hash"];
            pairs.Remove("hash");

            // Sort parameters alphabetically and create data check string
            var sortedParams = pairs.AllKeys
                .Where(k => k != null)
                .OrderBy(k => k)
                .Select(k => $"{k}={pairs[k]}")
                .ToArray();

            var dataCheckString = string.Join("\n", sortedParams);

            // Calculate secret key: HMAC-SHA256(bot_token, "WebAppData")
            using var hmac1 = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
            var secretKey = hmac1.ComputeHash(Encoding.UTF8.GetBytes(botToken));

            // Calculate hash: HMAC-SHA256(data_check_string, secret_key)
            using var hmac2 = new HMACSHA256(secretKey);
            var calculatedHash = hmac2.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
            var calculatedHashHex = BitConverter.ToString(calculatedHash).Replace("-", "").ToLowerInvariant();

            return calculatedHashHex == hash;
        }
        catch
        {
            return false;
        }
    }
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
    Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly ICacheService _cacheService;

    public JwtTokenService(IOptions<JwtSettings> settings, ICacheService cacheService)
    {
        _settings = settings.Value;
        _cacheService = cacheService;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FirstName),
            new Claim("telegram_id", user.TelegramId.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("is_verified", user.IsVerified.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var key = $"refresh_token:{refreshToken}";

        await _cacheService.SetAsync(key, userId.ToString(), TimeSpan.FromDays(7), cancellationToken);
        return refreshToken;
    }

    public async Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = $"refresh_token:{refreshToken}";
        var userIdStr = await _cacheService.GetAsync<string>(key, cancellationToken);

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return null;

        return userId;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = $"refresh_token:{refreshToken}";
        await _cacheService.RemoveAsync(key, cancellationToken);
    }
}
