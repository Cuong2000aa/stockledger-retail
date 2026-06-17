using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Products;

public class ProductDto
{
    public Guid Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? Category { get; set; }

    public ProductStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
