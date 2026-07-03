namespace StockLedgerRetail.Inventory;

public class StockReconciliationOptions
{
    public const string SectionName = "Inventory:Reconciliation";

    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// When true, only variant/warehouse pairs with recent stock activity are reconciled on most runs.
    /// </summary>
    public bool IncrementalOnly { get; set; } = true;

    public int IncrementalLookbackHours { get; set; } = 24;

    /// <summary>
    /// Hour in UTC when a full reconciliation run is performed once per day.
    /// </summary>
    public int FullReconciliationHourUtc { get; set; } = 2;
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
