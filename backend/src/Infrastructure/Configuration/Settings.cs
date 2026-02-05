namespace TelegramMarketplace.Infrastructure.Configuration;

public class TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
    public string BotUsername { get; set; } = string.Empty;
    public string WebAppUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
}

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

public class YooKassaSettings
{
    public string ShopId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public bool IsTestMode { get; set; } = true;
}

public class RobokassaSettings
{
    public string MerchantLogin { get; set; } = string.Empty;
    public string Password1 { get; set; } = string.Empty;
    public string Password2 { get; set; } = string.Empty;
    public bool IsTestMode { get; set; } = true;
}

public class CommissionSettings
{
    public decimal Percentage { get; set; } = 10;
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "TelegramMarketplace:";
}

public class FileStorageSettings
{
    public string BasePath { get; set; } = "uploads";
    public string BaseUrl { get; set; } = "/uploads";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
}
