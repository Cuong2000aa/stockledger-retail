using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Inventory;

public class InventoryDocumentLineDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public Guid? StockLotId { get; set; }

    public string? LotCode { get; set; }

    public List<string> Barcodes { get; set; } = new();

    public DateTime? ExpiryDate { get; set; }

    public string? Note { get; set; }
}

public class InventoryDocumentDto
{
    public Guid Id { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public InventoryDocumentType DocumentType { get; set; }

    public Guid? SourceWarehouseId { get; set; }

    public Guid? DestinationWarehouseId { get; set; }

    public InventoryDocumentStatus Status { get; set; }

    public DateTime DocumentDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? SourceSystem { get; set; }

    public string? Note { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? SubmittedBy { get; set; }

    public int RequiredApprovalSteps { get; set; }

    public int CompletedApprovalSteps { get; set; }

    public string? FirstApprovedBy { get; set; }

    public DateTime? FirstApprovedAt { get; set; }

    public TransferLifecycleStatus TransferLifecycleStatus { get; set; }

    public Guid? InTransitWarehouseId { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public List<InventoryDocumentLineDto> Lines { get; set; } = new();
}
