using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Inventory;

public class StockTransactionDto
{
    public Guid Id { get; set; }

    public string TransactionNo { get; set; } = string.Empty;

    public Guid DocumentId { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public string? SourceSystem { get; set; }

    public string? ReferenceNo { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public bool IsBarcode { get; set; }

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public Guid? CounterpartWarehouseId { get; set; }

    public string? CounterpartWarehouseCode { get; set; }

    public StockTransactionType TransactionType { get; set; }

    public decimal QuantityDelta { get; set; }

    public decimal BeforeQuantity { get; set; }

    public decimal AfterQuantity { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal? ExtendedCost =>
        UnitCost.HasValue ? UnitCost.Value * Math.Abs(QuantityDelta) : null;

    public DateTime TransactionDate { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<string> Barcodes { get; set; } = new();
}
