using Microsoft.Extensions.Options;
using TelegramMarketplace.Infrastructure.Configuration;
using TelegramMarketplace.Infrastructure.Payments.TelegramStars;

namespace TelegramMarketplace.WebAPI.Services;

public class TelegramWebhookService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramWebhookService> _logger;

    public TelegramWebhookService(
        IServiceProvider serviceProvider,
        IOptions<TelegramSettings> settings,
        ILogger<TelegramWebhookService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_settings.WebhookUrl))
        {
            _logger.LogWarning("Telegram webhook URL not configured. Telegram Stars payments will not work.");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var telegramClient = scope.ServiceProvider.GetRequiredService<ITelegramStarsClient>();

            var webhookUrl = _settings.WebhookUrl.TrimEnd('/') + "/api/telegram/webhook";
            var success = await telegramClient.SetWebhookAsync(webhookUrl, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Telegram webhook configured successfully: {Url}", webhookUrl);
            }
            else
            {
                _logger.LogError("Failed to configure Telegram webhook");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring Telegram webhook");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
