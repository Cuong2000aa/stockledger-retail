namespace StockLedgerRetail.Domain.Inventory;

public class StockLedgerAggregate
{
    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }

    public decimal LedgerQuantity { get; set; }
}

public class StockReconciliationMismatch
{
    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }

    public decimal LedgerQuantity { get; set; }

    public decimal CurrentStockQuantity { get; set; }

    public decimal Variance => CurrentStockQuantity - LedgerQuantity;
}

public class StockReconciliationResult
{
    public DateTime CheckedAt { get; set; }

    public int TotalPairsChecked { get; set; }

    public int MismatchCount { get; set; }

    public List<StockReconciliationMismatch> Mismatches { get; set; } = new();
}
