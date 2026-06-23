using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class Product : AuditedEntity
{
    public string ProductCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public Guid? BrandId { get; set; }

    public string? Category { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public Brand? BrandEntity { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
