using System.ComponentModel.DataAnnotations;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.ProductVariants;

public class CreateProductVariantDto
{
    [Required]
    public Guid ProductId { get; set; }

    public Guid? BrandId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Barcode { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Season { get; set; }

    [MaxLength(20)]
    public string? Unit { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    [Range(0, double.MaxValue)]
    public decimal? CostPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SellingPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SellingPriceBeforeVat { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SellingPriceAfterVat { get; set; }

    [Range(0, 100)]
    public decimal? VatRate { get; set; }

    public CostSource? CostSource { get; set; }

    public bool TrackLotExpiry { get; set; }

    public bool IsBarcode { get; set; }
}
