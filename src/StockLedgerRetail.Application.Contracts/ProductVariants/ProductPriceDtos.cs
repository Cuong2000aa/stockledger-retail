using System.ComponentModel.DataAnnotations;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.ProductVariants;

public class ProductPriceDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public PriceType PriceType { get; set; }

    public decimal PriceBeforeVat { get; set; }

    public decimal VatRate { get; set; }

    public decimal PriceAfterVat { get; set; }

    public string Currency { get; set; } = "VND";

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsCurrent { get; set; }

    public string? ChannelCode { get; set; }

    public string? ReferenceType { get; set; }
}

public class UpsertProductPriceDto
{
    public PriceType PriceType { get; set; }

    /// <summary>Price before VAT is authoritative; after VAT is derived on save when mismatched.</summary>
    [Range(0, double.MaxValue)]
    public decimal PriceBeforeVat { get; set; }

    [Range(0, 100)]
    public decimal VatRate { get; set; }

    /// <summary>Optional preview from client; server validates consistency with <see cref="PriceBeforeVat"/>.</summary>
    [Range(0, double.MaxValue)]
    public decimal? PriceAfterVat { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    [MaxLength(50)]
    public string? ChannelCode { get; set; }

    [MaxLength(50)]
    public string? ReferenceType { get; set; }
}
