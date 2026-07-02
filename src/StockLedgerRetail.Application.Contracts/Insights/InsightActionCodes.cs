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

    public const string MarkdownCandidateReview = "markdown_candidate_review";
    public const string MarkdownCandidateExecute = "markdown_candidate_execute";

    public const string PromotionRiskReview = "promotion_risk_review";
    public const string PromotionRiskTightStock = "promotion_risk_tight_stock";

    public const string ReorderRiskUrgent = "reorder_risk_urgent";
    public const string ReorderRiskPlan = "reorder_risk_plan";

    public const string TrendReview = "trend_review";

    public const string BrokenSizeRunConsolidate = "broken_size_run_consolidate";

    public const string SeasonClearanceMarkdown = "season_clearance_markdown";
}
