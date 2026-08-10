namespace StockLedgerRetail.Insights;

public enum TransferRebalanceMode
{
    Heuristic = 0,
    MinCostFlow = 1
}

public class TransferRebalanceOptions
{
    public const string SectionName = "Inventory:TransferRebalance";

    /// <summary>Heuristic (scored multi-pass) or MinCostFlow. Defaults to Heuristic.</summary>
    public TransferRebalanceMode Mode { get; set; } = TransferRebalanceMode.Heuristic;

    /// <summary>Weight for destination cover gap (days below target).</summary>
    public decimal CoverGapWeight { get; set; } = 3m;

    /// <summary>Weight for margin opportunity (selling − cost) × quantity.</summary>
    public decimal MarginWeight { get; set; } = 1m;

    /// <summary>Weight for feasible transfer quantity.</summary>
    public decimal QuantityWeight { get; set; } = 0.1m;

    /// <summary>Penalty when source and destination regions differ.</summary>
    public decimal CrossRegionPenalty { get; set; } = 5m;

    /// <summary>
    /// Soft timeout for MinCostFlow (milliseconds). On exceed or failure, fall back to Heuristic.
    /// </summary>
    public int MinCostFlowTimeoutMs { get; set; } = 2000;

    /// <summary>Max source×destination edge candidates per SKU before falling back to Heuristic.</summary>
    public int MaxEdgesPerSku { get; set; } = 500;
}
