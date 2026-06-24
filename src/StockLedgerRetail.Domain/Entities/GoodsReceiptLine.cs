namespace StockLedgerRetail.Domain.Entities;

public class GoodsReceiptLine
{
    public Guid Id { get; set; }

    public Guid GoodsReceiptId { get; set; }

    public Guid PurchaseOrderLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? LotCode { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? Note { get; set; }

    public GoodsReceipt? GoodsReceipt { get; set; }

    public PurchaseOrderLine? PurchaseOrderLine { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}
