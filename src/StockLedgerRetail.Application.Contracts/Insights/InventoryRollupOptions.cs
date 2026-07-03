namespace StockLedgerRetail.Insights;

public class InventoryRollupOptions
{
    public const string SectionName = "Inventory:Rollups";

    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 360;
}
