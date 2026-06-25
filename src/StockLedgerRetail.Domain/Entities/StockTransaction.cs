using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class StockTransaction
{
    public Guid Id { get; set; }

    public string TransactionNo { get; set; } = string.Empty;

    public Guid DocumentId { get; set; }

    public Guid DocumentLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }

    public StockTransactionType TransactionType { get; set; }

    public decimal QuantityDelta { get; set; }

    public decimal BeforeQuantity { get; set; }

    public decimal AfterQuantity { get; set; }

    /// <summary>Giá vốn đơn vị tại thời điểm giao dịch (COGS cho xuất, giá nhập cho nhập có cost).</summary>
    public decimal? UnitCost { get; set; }

    public DateTime TransactionDate { get; set; }

    /// <summary>Denormalized từ phiếu nguồn — tra cứu lịch sử nhanh.</summary>
    public string DocumentNo { get; set; } = string.Empty;

    public string? SourceSystem { get; set; }

    public string? ReferenceNo { get; set; }

    /// <summary>Kho đối ứng (nguồn khi nhập chuyển, đích khi xuất chuyển).</summary>
    public Guid? CounterpartWarehouseId { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public InventoryDocument Document { get; set; } = null!;

    public InventoryDocumentLine DocumentLine { get; set; } = null!;

    public ProductVariant ProductVariant { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;

    public Warehouse? CounterpartWarehouse { get; set; }

    public ICollection<CurrentStock> CurrentStocks { get; set; } = new List<CurrentStock>();

    public ICollection<StockTransactionBarcode> Barcodes { get; set; } = new List<StockTransactionBarcode>();
}
