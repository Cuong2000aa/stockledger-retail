using StockLedgerRetail.Enums;

namespace StockLedgerRetail.PurchaseOrders;

public class PurchaseOrderLineDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public decimal OrderedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal RemainingQuantity => OrderedQuantity - ReceivedQuantity;

    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }
}

public class PurchaseOrderDto
{
    public Guid Id { get; set; }

    public string PoNo { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public PurchaseOrderStatus Status { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int RequiredApprovalSteps { get; set; }

    public int CompletedApprovalSteps { get; set; }

    public string? FirstApprovedBy { get; set; }

    public DateTime? FirstApprovedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public class CreatePurchaseOrderLineDto
{
    public Guid ProductVariantId { get; set; }

    public decimal OrderedQuantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }
}

public class CreatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }

    public Guid WarehouseId { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public List<CreatePurchaseOrderLineDto> Lines { get; set; } = new();
}
