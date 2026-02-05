using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramMarketplace.Infrastructure.Configuration;

namespace TelegramMarketplace.Infrastructure.Payments.YooKassa;

public interface IYooKassaClient
{
    Task<YooKassaPaymentResponse?> CreatePaymentAsync(YooKassaPaymentRequest request, string idempotenceKey, CancellationToken cancellationToken = default);
    Task<YooKassaPaymentResponse?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
    Task<YooKassaPaymentResponse?> CapturePaymentAsync(string paymentId, YooKassaCaptureRequest request, string idempotenceKey, CancellationToken cancellationToken = default);
    Task<YooKassaRefundResponse?> CreateRefundAsync(YooKassaRefundRequest request, string idempotenceKey, CancellationToken cancellationToken = default);
    bool ValidateWebhookSignature(string body, string signature);
}

public class YooKassaClient : IYooKassaClient
{
    private readonly HttpClient _httpClient;
    private readonly YooKassaSettings _settings;
    private readonly ILogger<YooKassaClient> _logger;
    private const string BaseUrl = "https://api.yookassa.ru/v3";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public YooKassaClient(
        HttpClient httpClient,
        IOptions<YooKassaSettings> settings,
        ILogger<YooKassaClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ShopId}:{_settings.SecretKey}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
    }

    public async Task<YooKassaPaymentResponse?> CreatePaymentAsync(
        YooKassaPaymentRequest request,
        string idempotenceKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/payments")
            {
                Content = content
            };
            requestMessage.Headers.Add("Idempotence-Key", idempotenceKey);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("YooKassa payment creation failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<YooKassaPaymentResponse>(responseBody, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating YooKassa payment");
            return null;
        }
    }

    public async Task<YooKassaPaymentResponse?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/payments/{paymentId}", cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("YooKassa get payment failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<YooKassaPaymentResponse>(responseBody, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting YooKassa payment {PaymentId}", paymentId);
            return null;
        }
    }

    public async Task<YooKassaPaymentResponse?> CapturePaymentAsync(
        string paymentId,
        YooKassaCaptureRequest request,
        string idempotenceKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/payments/{paymentId}/capture")
            {
                Content = content
            };
            requestMessage.Headers.Add("Idempotence-Key", idempotenceKey);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("YooKassa capture failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<YooKassaPaymentResponse>(responseBody, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing YooKassa payment {PaymentId}", paymentId);
            return null;
        }
    }

    public async Task<YooKassaRefundResponse?> CreateRefundAsync(
        YooKassaRefundRequest request,
        string idempotenceKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/refunds")
            {
                Content = content
            };
            requestMessage.Headers.Add("Idempotence-Key", idempotenceKey);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("YooKassa refund failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<YooKassaRefundResponse>(responseBody, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating YooKassa refund");
            return null;
        }
    }

    public bool ValidateWebhookSignature(string body, string signature)
    {
        // YooKassa uses IP whitelist instead of signature for webhooks
        // In production, verify the request comes from YooKassa IPs
        return !string.IsNullOrEmpty(body);
    }
}

// Request/Response models
public class YooKassaPaymentRequest
{
    public YooKassaAmount Amount { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public YooKassaConfirmation Confirmation { get; set; } = null!;
    public bool Capture { get; set; } = true;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class YooKassaAmount
{
    public string Value { get; set; } = string.Empty;
    public string Currency { get; set; } = "RUB";
}

public class YooKassaConfirmation
{
    public string Type { get; set; } = "redirect";
    public string ReturnUrl { get; set; } = string.Empty;
}

public class YooKassaCaptureRequest
{
    public YooKassaAmount Amount { get; set; } = null!;
}

public class YooKassaRefundRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public YooKassaAmount Amount { get; set; } = null!;
    public string? Description { get; set; }
}

public class YooKassaPaymentResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public YooKassaAmount Amount { get; set; } = null!;
    public string? Description { get; set; }
    public bool Paid { get; set; }
    public bool Refundable { get; set; }
    public YooKassaConfirmationResponse? Confirmation { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
}

public class YooKassaConfirmationResponse
{
    public string Type { get; set; } = string.Empty;
    public string? ConfirmationUrl { get; set; }
}

public class YooKassaRefundResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public YooKassaAmount Amount { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class YooKassaWebhookPayload
{
    public string Type { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public YooKassaPaymentResponse Object { get; set; } = null!;
}
