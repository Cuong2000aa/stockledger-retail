using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; }

    public string PoNo { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public Guid WarehouseId { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int RequiredApprovalSteps { get; set; } = 1;

    public int CompletedApprovalSteps { get; set; }

    public string? FirstApprovedBy { get; set; }

    public DateTime? FirstApprovedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Supplier? Supplier { get; set; }

    public Warehouse? Warehouse { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();

    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}
