namespace StockLedgerRetail.Insights;

public static class InsightActionCodes
{
    public const string DeadStockCriticalMarkdown = "dead_stock_critical_markdown";
    public const string DeadStockMarkdownOrTransfer = "dead_stock_markdown_or_transfer";
    public const string DeadStockReview = "dead_stock_review";

    public const string VelocityReplenishUrgent = "velocity_replenish_urgent";
    public const string VelocityReplenishPlan = "velocity_replenish_plan";
    public const string VelocityMonitor = "velocity_monitor";
    public const string VelocityNoDemandReview = "velocity_no_demand_review";

    public const string TransferExecute = "transfer_execute";
}
