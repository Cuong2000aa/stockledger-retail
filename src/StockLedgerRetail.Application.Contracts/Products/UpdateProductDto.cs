using System.ComponentModel.DataAnnotations;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Products;

public class UpdateProductDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Brand { get; set; }

    public Guid? BrandId { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public ProductStatus Status { get; set; }
}
