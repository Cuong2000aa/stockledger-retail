using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class InventoryDocument
{
    public Guid Id { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public InventoryDocumentType DocumentType { get; set; }

    public Guid? SourceWarehouseId { get; set; }

    public Guid? DestinationWarehouseId { get; set; }

    public InventoryDocumentStatus Status { get; set; } = InventoryDocumentStatus.Draft;

    public DateTime DocumentDate { get; set; }

    public string? ReferenceNo { get; set; }

    /// <summary>
    /// External system that originated this document (e.g. POS, OMS).
    /// </summary>
    public string? SourceSystem { get; set; }

    public string? Note { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? SubmittedBy { get; set; }

    public int RequiredApprovalSteps { get; set; } = 1;

    public int CompletedApprovalSteps { get; set; }

    public string? FirstApprovedBy { get; set; }

    public DateTime? FirstApprovedAt { get; set; }

    public TransferLifecycleStatus TransferLifecycleStatus { get; set; } = TransferLifecycleStatus.None;

    public Guid? InTransitWarehouseId { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public uint RowVersion { get; set; }

    public Warehouse? SourceWarehouse { get; set; }

    public Warehouse? DestinationWarehouse { get; set; }

    public Warehouse? InTransitWarehouse { get; set; }

    public ICollection<InventoryDocumentLine> Lines { get; set; } = new List<InventoryDocumentLine>();

    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
