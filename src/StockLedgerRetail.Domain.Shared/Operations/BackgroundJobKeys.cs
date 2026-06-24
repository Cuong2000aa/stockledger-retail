namespace StockLedgerRetail.Operations;

public static class BackgroundJobKeys
{
    public const string InsightSnapshots = "insight_snapshots";
    public const string StockReconciliation = "stock_reconciliation";
}

public static class BackgroundJobStatuses
{
    public const string Idle = "idle";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}

public static class BackgroundJobTriggers
{
    public const string Scheduled = "scheduled";
    public const string Manual = "manual";
}
