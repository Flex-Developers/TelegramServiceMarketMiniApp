using TelegramMarketplace.Domain.Common;
using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.Domain.Entities;

public class PromoCode : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal? MinOrderAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public int? MaxUsageCount { get; private set; }
    public int CurrentUsageCount { get; private set; }
    public int? MaxUsagePerUser { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PromoCode() { }

    public static PromoCode Create(
        string code,
        DiscountType discountType,
        decimal discountValue,
        decimal? minOrderAmount = null,
        decimal? maxDiscountAmount = null,
        int? maxUsageCount = null,
        int? maxUsagePerUser = null,
        DateTime? validFrom = null,
        DateTime? validTo = null)
    {
        return new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant(),
            DiscountType = discountType,
            DiscountValue = discountValue,
            MinOrderAmount = minOrderAmount,
            MaxDiscountAmount = maxDiscountAmount,
            MaxUsageCount = maxUsageCount,
            CurrentUsageCount = 0,
            MaxUsagePerUser = maxUsagePerUser,
            ValidFrom = validFrom,
            ValidTo = validTo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsValid()
    {
        if (!IsActive) return false;
        if (MaxUsageCount.HasValue && CurrentUsageCount >= MaxUsageCount.Value) return false;
        if (ValidFrom.HasValue && DateTime.UtcNow < ValidFrom.Value) return false;
        if (ValidTo.HasValue && DateTime.UtcNow > ValidTo.Value) return false;
        return true;
    }

    public decimal CalculateDiscount(decimal orderAmount)
    {
        if (MinOrderAmount.HasValue && orderAmount < MinOrderAmount.Value)
            return 0;

        var discount = DiscountType == DiscountType.Percentage
            ? orderAmount * (DiscountValue / 100)
            : DiscountValue;

        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
            discount = MaxDiscountAmount.Value;

        return discount;
    }

    public void IncrementUsage()
    {
        CurrentUsageCount++;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
