using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Application.DTOs;

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentProvider Provider,
    PaymentStatus Status,
    string? ExternalId,
    string? ConfirmationUrl,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record CreatePaymentRequest(
    Guid OrderId,
    PaymentProvider Provider,
    string? ReturnUrl
);

public record PaymentResultDto(
    Guid PaymentId,
    Guid OrderId,
    PaymentStatus Status,
    string? ConfirmationUrl,
    string? Message
);

// YooKassa specific
public record YooKassaWebhookDto(
    string Type,
    string Event,
    YooKassaPaymentObject Object
);

public record YooKassaPaymentObject(
    string Id,
    string Status,
    YooKassaAmount Amount,
    string? Description,
    YooKassaMetadata? Metadata,
    bool Paid,
    bool Refundable,
    DateTime CreatedAt,
    DateTime? CapturedAt
);

public record YooKassaAmount(
    string Value,
    string Currency
);

public record YooKassaMetadata(
    string? OrderId,
    string? UserId
);

// Robokassa specific
public record RobokassaCallbackDto(
    decimal OutSum,
    int InvId,
    string SignatureValue,
    string? Shp_orderId
);

// Telegram Stars specific
public record TelegramStarsPaymentDto(
    string InvoiceId,
    int Stars,
    string Payload,
    string? TelegramPaymentChargeId
);

// Telegram Webhook Update types
public class TelegramUpdate
{
    public long UpdateId { get; set; }
    public TelegramPreCheckoutQuery? PreCheckoutQuery { get; set; }
    public TelegramMessage? Message { get; set; }
}

public class TelegramPreCheckoutQuery
{
    public string Id { get; set; } = string.Empty;
    public TelegramUser From { get; set; } = new();
    public string Currency { get; set; } = string.Empty;
    public int TotalAmount { get; set; }
    public string InvoicePayload { get; set; } = string.Empty;
}

public class TelegramMessage
{
    public int MessageId { get; set; }
    public TelegramUser? From { get; set; }
    public TelegramSuccessfulPaymentInfo? SuccessfulPayment { get; set; }
}

public class TelegramUser
{
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? Username { get; set; }
}

public class TelegramSuccessfulPaymentInfo
{
    public string Currency { get; set; } = string.Empty;
    public int TotalAmount { get; set; }
    public string InvoicePayload { get; set; } = string.Empty;
    public string TelegramPaymentChargeId { get; set; } = string.Empty;
    public string ProviderPaymentChargeId { get; set; } = string.Empty;
}
