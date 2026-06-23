namespace StockLedgerRetail.Inventory;

public class StockReconciliationOptions
{
    public const string SectionName = "Inventory:Reconciliation";

    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 60;
}

public class StockReconciliationMismatchDto
{
    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }

    public decimal LedgerQuantity { get; set; }

    public decimal CurrentStockQuantity { get; set; }

    public decimal Variance { get; set; }
}

public class StockReconciliationResultDto
{
    public DateTime CheckedAt { get; set; }

    public int TotalPairsChecked { get; set; }

    public int MismatchCount { get; set; }

    public List<StockReconciliationMismatchDto> Mismatches { get; set; } = new();
}
