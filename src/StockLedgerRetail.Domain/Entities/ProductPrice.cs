using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

/// <summary>
/// Lịch sử giá bán theo SKU với hiệu lực theo thời gian.
/// </summary>
public class ProductPrice : AuditedEntity
{
    public Guid ProductVariantId { get; set; }

    public PriceType PriceType { get; set; } = PriceType.Regular;

    public decimal PriceBeforeVat { get; set; }

    public decimal VatRate { get; set; }

    public decimal PriceAfterVat { get; set; }

    public string Currency { get; set; } = "VND";

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsCurrent { get; set; } = true;

    public string? ChannelCode { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;
}
