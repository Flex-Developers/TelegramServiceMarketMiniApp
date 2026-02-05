using TelegramMarketplace.Domain.Common;
using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "RUB";
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? ExternalId { get; private set; }
    public string? ConfirmationUrl { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Metadata { get; private set; }

    // Navigation property
    public virtual Order Order { get; private set; } = null!;

    private Payment() { }

    public static Payment Create(
        Guid orderId,
        decimal amount,
        PaymentProvider provider,
        string currency = "RUB")
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Currency = currency,
            Provider = provider,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetExternalId(string externalId)
    {
        ExternalId = externalId;
    }

    public void SetConfirmationUrl(string url)
    {
        ConfirmationUrl = url;
    }

    public void MarkAsWaitingForCapture()
    {
        Status = PaymentStatus.WaitingForCapture;
    }

    public void MarkAsCompleted()
    {
        Status = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string? errorCode = null, string? errorMessage = null)
    {
        Status = PaymentStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public void MarkAsCancelled()
    {
        Status = PaymentStatus.Cancelled;
    }

    public void MarkAsRefunding()
    {
        Status = PaymentStatus.Refunding;
    }

    public void MarkAsRefunded()
    {
        Status = PaymentStatus.Refunded;
    }

    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
    }
}
