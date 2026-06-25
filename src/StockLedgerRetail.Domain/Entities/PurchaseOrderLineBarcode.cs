namespace StockLedgerRetail.Domain.Entities;

public class PurchaseOrderLineBarcode
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderLineId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
}
