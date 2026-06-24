namespace StockLedgerRetail.Insights;

public class InsightSnapshotOptions
{
    public const string SectionName = "Inventory:Insights";

    public bool Enabled { get; set; } = true;

    public bool UseSnapshotOnRead { get; set; } = true;

    public int RefreshIntervalMinutes { get; set; } = 30;

    public int MaxSnapshotAgeMinutes { get; set; } = 45;
}
