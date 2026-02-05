using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramMarketplace.Infrastructure.Configuration;

namespace TelegramMarketplace.Infrastructure.Payments.TelegramStars;

public interface ITelegramStarsClient
{
    Task<TelegramInvoiceResponse?> CreateInvoiceLinkAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<bool> AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task<bool> RefundStarPaymentAsync(long userId, string telegramPaymentChargeId, CancellationToken cancellationToken = default);
    Task<bool> SetWebhookAsync(string webhookUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteWebhookAsync(CancellationToken cancellationToken = default);
}

public class TelegramStarsClient : ITelegramStarsClient
{
    private readonly HttpClient _httpClient;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramStarsClient> _logger;
    private readonly string _baseUrl;

    public TelegramStarsClient(
        HttpClient httpClient,
        IOptions<TelegramSettings> settings,
        ILogger<TelegramStarsClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _baseUrl = $"https://api.telegram.org/bot{_settings.BotToken}";
    }

    public async Task<TelegramInvoiceResponse?> CreateInvoiceLinkAsync(
        TelegramInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                title = request.Title,
                description = request.Description,
                payload = request.Payload,
                currency = "XTR", // Telegram Stars currency code
                prices = new[]
                {
                    new { label = request.Title, amount = request.Amount }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/createInvoiceLink", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Telegram invoice creation failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return null;
            }

            var result = JsonSerializer.Deserialize<TelegramApiResponse<string>>(responseBody);
            if (result?.Ok != true)
            {
                _logger.LogError("Telegram API error: {Description}", result?.Description);
                return null;
            }

            return new TelegramInvoiceResponse
            {
                InvoiceLink = result.Result!,
                Payload = request.Payload
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Telegram invoice");
            return null;
        }
    }

    public async Task<bool> AnswerPreCheckoutQueryAsync(
        string preCheckoutQueryId,
        bool ok,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new Dictionary<string, object>
            {
                ["pre_checkout_query_id"] = preCheckoutQueryId,
                ["ok"] = ok
            };

            if (!ok && !string.IsNullOrEmpty(errorMessage))
            {
                payload["error_message"] = errorMessage;
            }

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/answerPreCheckoutQuery", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error answering pre-checkout query {QueryId}", preCheckoutQueryId);
            return false;
        }
    }

    public async Task<bool> RefundStarPaymentAsync(
        long userId,
        string telegramPaymentChargeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                user_id = userId,
                telegram_payment_charge_id = telegramPaymentChargeId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/refundStarPayment", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Telegram refund failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return false;
            }

            var result = JsonSerializer.Deserialize<TelegramApiResponse<bool>>(responseBody);
            return result?.Ok == true && result.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding Telegram Stars payment");
            return false;
        }
    }

    public async Task<bool> SetWebhookAsync(
        string webhookUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                url = webhookUrl,
                allowed_updates = new[] { "pre_checkout_query", "message" }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Setting Telegram webhook to: {Url}", webhookUrl);

            var response = await _httpClient.PostAsync($"{_baseUrl}/setWebhook", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Telegram setWebhook failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return false;
            }

            var result = JsonSerializer.Deserialize<TelegramApiResponse<bool>>(responseBody);
            if (result?.Ok == true)
            {
                _logger.LogInformation("Telegram webhook set successfully");
                return true;
            }

            _logger.LogError("Telegram setWebhook failed: {Description}", result?.Description);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Telegram webhook");
            return false;
        }
    }

    public async Task<bool> DeleteWebhookAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/deleteWebhook", null, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<TelegramApiResponse<bool>>(responseBody);
            return result?.Ok == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Telegram webhook");
            return false;
        }
    }
}

public class TelegramInvoiceRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int Amount { get; set; } // In Telegram Stars
}

public class TelegramInvoiceResponse
{
    public string InvoiceLink { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}

public class TelegramApiResponse<T>
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class TelegramSuccessfulPayment
{
    public string Currency { get; set; } = string.Empty;
    public int TotalAmount { get; set; }
    public string InvoicePayload { get; set; } = string.Empty;
    public string TelegramPaymentChargeId { get; set; } = string.Empty;
    public string ProviderPaymentChargeId { get; set; } = string.Empty;
}
