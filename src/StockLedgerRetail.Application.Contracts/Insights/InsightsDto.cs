namespace StockLedgerRetail.Insights;

public class DeadStockInsightDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityAvailable { get; set; }

    public DateTime? LastOutboundAt { get; set; }

    public int DaysWithoutOutbound { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? EstimatedCostValue { get; set; }

    public string Severity { get; set; } = "info";

    public string RuleCode { get; set; } = "dead_stock";

    public Guid? BrandId { get; set; }

    public string RecommendedActionCode { get; set; } = string.Empty;

    public Dictionary<string, string> RecommendationParams { get; set; } = new();

    public InsightRecommendationDto Recommendation { get; set; } = new();
}

public class SalesVelocityInsightDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityAvailable { get; set; }

    public decimal OutboundQuantity { get; set; }

    public decimal AverageDailyOutbound { get; set; }

    public decimal? EstimatedDaysOfCover { get; set; }

    public DateTime? LastOutboundAt { get; set; }

    public int LookbackDays { get; set; }

    public string Severity { get; set; } = "info";

    public string RuleCode { get; set; } = "sales_velocity";

    public Guid? BrandId { get; set; }

    public string RecommendedActionCode { get; set; } = string.Empty;

    public Dictionary<string, string> RecommendationParams { get; set; } = new();

    public InsightRecommendationDto Recommendation { get; set; } = new();
}

public class TransferSuggestionDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid SourceWarehouseId { get; set; }

    public string SourceWarehouseCode { get; set; } = string.Empty;

    public string SourceWarehouseName { get; set; } = string.Empty;

    public Guid DestinationWarehouseId { get; set; }

    public string DestinationWarehouseCode { get; set; } = string.Empty;

    public string DestinationWarehouseName { get; set; } = string.Empty;

    public decimal SuggestedQuantity { get; set; }

    public decimal SourceAvailable { get; set; }

    public decimal DestinationAvailable { get; set; }

    public decimal DestinationAverageDailyOutbound { get; set; }

    public decimal? DestinationDaysOfCover { get; set; }

    public string Severity { get; set; } = "warning";

    public string RuleCode { get; set; } = "transfer_rebalance";

    public string RecommendedActionCode { get; set; } = string.Empty;

    public Dictionary<string, string> RecommendationParams { get; set; } = new();

    public InsightRecommendationDto Recommendation { get; set; } = new();
}
