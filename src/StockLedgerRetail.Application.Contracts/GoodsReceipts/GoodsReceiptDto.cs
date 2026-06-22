using StockLedgerRetail.Enums;

namespace StockLedgerRetail.GoodsReceipts;

public class GoodsReceiptLineDto
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public decimal ReceivedQuantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }
}

public class GoodsReceiptDto
{
    public Guid Id { get; set; }

    public string GrNo { get; set; } = string.Empty;

    public Guid PurchaseOrderId { get; set; }

    public string PoNo { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public GoodsReceiptStatus Status { get; set; }

    public DateTime ReceiptDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public Guid? InventoryDocumentId { get; set; }

    public string? InventoryDocumentNo { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public List<GoodsReceiptLineDto> Lines { get; set; } = new();
}

public class CreateGoodsReceiptLineDto
{
    public Guid PurchaseOrderLineId { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public string? Note { get; set; }
}

public class CreateGoodsReceiptDto
{
    public Guid PurchaseOrderId { get; set; }

    public DateTime? ReceiptDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public List<CreateGoodsReceiptLineDto> Lines { get; set; } = new();
}
