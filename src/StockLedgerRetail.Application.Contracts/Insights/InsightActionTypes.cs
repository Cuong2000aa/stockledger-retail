namespace StockLedgerRetail.Insights;

public static class InsightActionTypes
{
    public const string Monitor = "monitor";
    public const string Review = "review";
    public const string Markdown = "markdown";
    public const string Replenish = "replenish";
    public const string Transfer = "transfer";
}

public static class InsightCtaKinds
{
    public const string Navigate = "navigate";
    public const string Api = "api";
}

public static class InsightApiOperations
{
    public const string CreateTransfer = "create_transfer";
}
