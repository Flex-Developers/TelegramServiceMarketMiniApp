namespace TelegramMarketplace.Application.DTOs;

public record TelegramAuthRequest(
    string InitData
);

public record AuthResultDto(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    UserDto? User,
    string? Error
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record TelegramInitData(
    long UserId,
    string FirstName,
    string? LastName,
    string? Username,
    string? PhotoUrl,
    string? LanguageCode,
    long AuthDate,
    string Hash
);
