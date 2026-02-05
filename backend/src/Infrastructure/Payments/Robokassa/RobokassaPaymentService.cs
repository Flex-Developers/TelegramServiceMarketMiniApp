using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramMarketplace.Infrastructure.Configuration;

namespace TelegramMarketplace.Infrastructure.Payments.Robokassa;

public interface IRobokassaClient
{
    string GeneratePaymentUrl(RobokassaPaymentRequest request);
    bool ValidateResultSignature(decimal outSum, int invId, string signatureValue, Dictionary<string, string>? shpParams = null);
    bool ValidateSuccessSignature(decimal outSum, int invId, string signatureValue, Dictionary<string, string>? shpParams = null);
}

public class RobokassaClient : IRobokassaClient
{
    private readonly RobokassaSettings _settings;
    private readonly ILogger<RobokassaClient> _logger;

    public RobokassaClient(IOptions<RobokassaSettings> settings, ILogger<RobokassaClient> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public string GeneratePaymentUrl(RobokassaPaymentRequest request)
    {
        var baseUrl = _settings.IsTestMode
            ? "https://auth.robokassa.ru/Merchant/Index.aspx"
            : "https://auth.robokassa.ru/Merchant/Index.aspx";

        // Format amount with 2 decimal places
        var outSum = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        // Generate signature: MerchantLogin:OutSum:InvId:Password1[:Shp_xxx]
        var signatureBase = $"{_settings.MerchantLogin}:{outSum}:{request.InvoiceId}:{_settings.Password1}";

        // Add custom parameters (Shp_xxx) in alphabetical order
        if (request.CustomParams?.Any() == true)
        {
            var sortedParams = request.CustomParams
                .OrderBy(p => p.Key)
                .Select(p => $"{p.Key}={p.Value}");
            signatureBase += ":" + string.Join(":", sortedParams);
        }

        var signature = CalculateMD5(signatureBase);

        var queryParams = new List<string>
        {
            $"MerchantLogin={Uri.EscapeDataString(_settings.MerchantLogin)}",
            $"OutSum={outSum}",
            $"InvId={request.InvoiceId}",
            $"Description={Uri.EscapeDataString(request.Description)}",
            $"SignatureValue={signature}",
            $"Culture=ru"
        };

        if (_settings.IsTestMode)
        {
            queryParams.Add("IsTest=1");
        }

        // Add custom parameters
        if (request.CustomParams?.Any() == true)
        {
            foreach (var param in request.CustomParams)
            {
                queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value)}");
            }
        }

        return $"{baseUrl}?{string.Join("&", queryParams)}";
    }

    public bool ValidateResultSignature(decimal outSum, int invId, string signatureValue, Dictionary<string, string>? shpParams = null)
    {
        // Result URL signature: OutSum:InvId:Password2[:Shp_xxx]
        var outSumStr = outSum.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var signatureBase = $"{outSumStr}:{invId}:{_settings.Password2}";

        if (shpParams?.Any() == true)
        {
            var sortedParams = shpParams
                .Where(p => p.Key.StartsWith("Shp_", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key)
                .Select(p => $"{p.Key}={p.Value}");
            signatureBase += ":" + string.Join(":", sortedParams);
        }

        var calculatedSignature = CalculateMD5(signatureBase);
        var isValid = string.Equals(calculatedSignature, signatureValue, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
        {
            _logger.LogWarning("Invalid Robokassa result signature. Expected: {Expected}, Got: {Got}",
                calculatedSignature, signatureValue);
        }

        return isValid;
    }

    public bool ValidateSuccessSignature(decimal outSum, int invId, string signatureValue, Dictionary<string, string>? shpParams = null)
    {
        // Success URL signature: OutSum:InvId:Password1[:Shp_xxx]
        var outSumStr = outSum.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var signatureBase = $"{outSumStr}:{invId}:{_settings.Password1}";

        if (shpParams?.Any() == true)
        {
            var sortedParams = shpParams
                .Where(p => p.Key.StartsWith("Shp_", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key)
                .Select(p => $"{p.Key}={p.Value}");
            signatureBase += ":" + string.Join(":", sortedParams);
        }

        var calculatedSignature = CalculateMD5(signatureBase);
        return string.Equals(calculatedSignature, signatureValue, StringComparison.OrdinalIgnoreCase);
    }

    private static string CalculateMD5(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

public class RobokassaPaymentRequest
{
    public decimal Amount { get; set; }
    public int InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string>? CustomParams { get; set; }
}
