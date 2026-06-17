using StockLedgerRetail.Enums;

namespace StockLedgerRetail.ProductVariants;

public class ProductVariantDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? Season { get; set; }

    public string? Unit { get; set; }

    public ProductStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
