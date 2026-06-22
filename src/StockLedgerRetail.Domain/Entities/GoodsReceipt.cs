using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class GoodsReceipt
{
    public Guid Id { get; set; }

    public string GrNo { get; set; } = string.Empty;

    public Guid PurchaseOrderId { get; set; }

    public Guid WarehouseId { get; set; }

    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

    public DateTime ReceiptDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public Guid? InventoryDocumentId { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public Warehouse? Warehouse { get; set; }

    public InventoryDocument? InventoryDocument { get; set; }

    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}
