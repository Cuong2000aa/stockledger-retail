namespace StockLedgerRetail.Insights;

public class InsightSnapshotOptions
{
    public const string SectionName = "Inventory:Insights";

    public bool Enabled { get; set; } = true;

    public bool UseSnapshotOnRead { get; set; } = true;

    public int RefreshIntervalMinutes { get; set; } = 30;

    public int MaxSnapshotAgeMinutes { get; set; } = 45;

    /// <summary>
    /// When true, brand scopes are limited to active brands that currently have on-hand stock.
    /// </summary>
    public bool RefreshOnlyBrandsWithStock { get; set; } = true;

    /// <summary>
    /// When true, scopes whose executive-summary snapshot is still within MaxSnapshotAgeMinutes are skipped.
    /// </summary>
    public bool SkipFreshSnapshots { get; set; } = true;

    /// <summary>
    /// Maximum brand scopes refreshed per job run. 0 means no limit.
    /// </summary>
    public int MaxBrandsPerRun { get; set; } = 100;

    /// <summary>
    /// When false, the global scope is not fully recomputed; global executive summary is aggregated from brand snapshots.
    /// </summary>
    public bool RefreshGlobalScope { get; set; }

    /// <summary>
    /// Number of brand scopes processed concurrently. Each worker uses its own DI scope.
    /// </summary>
    public int MaxConcurrentBrandScopes { get; set; } = 2;
}
