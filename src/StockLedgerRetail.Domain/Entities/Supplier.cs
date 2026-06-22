using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class Supplier : AuditedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public SupplierStatus Status { get; set; } = SupplierStatus.Active;

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
