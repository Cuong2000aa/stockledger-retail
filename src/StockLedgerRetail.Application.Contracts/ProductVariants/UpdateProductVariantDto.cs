using System.ComponentModel.DataAnnotations;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.ProductVariants;

public class UpdateProductVariantDto
{
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

    public ProductStatus Status { get; set; }
}
