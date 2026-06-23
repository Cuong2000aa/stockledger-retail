using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class Brand : AuditedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public BrandStatus Status { get; set; } = BrandStatus.Active;

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
