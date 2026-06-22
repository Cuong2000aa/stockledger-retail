namespace StockLedgerRetail.Domain.Entities;

public class PurchaseOrderLine
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal OrderedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}
