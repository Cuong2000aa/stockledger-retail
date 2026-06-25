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

    public decimal? CurrentSellingPriceBeforeVat { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public decimal? EstimatedRevenueValue { get; set; }

    public decimal? EstimatedMarginValue { get; set; }

    public decimal? MarginRate { get; set; }

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

    public decimal? CostPrice { get; set; }

    public decimal? CurrentSellingPriceBeforeVat { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public decimal? RevenueOpportunity { get; set; }

    public decimal? MarginPerUnit { get; set; }

    public decimal? InventoryValue { get; set; }

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

    public decimal? SourceCostPrice { get; set; }

    public decimal? CurrentSellingPriceBeforeVat { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public decimal? TransferValue { get; set; }

    public decimal? MarginOpportunity { get; set; }

    public string Severity { get; set; } = "warning";

    public string RuleCode { get; set; } = "transfer_rebalance";

    public string RecommendedActionCode { get; set; } = string.Empty;

    public Dictionary<string, string> RecommendationParams { get; set; } = new();

    public InsightRecommendationDto Recommendation { get; set; } = new();
}

public class MarkdownCandidateInsightDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public int DaysWithoutOutbound { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? CurrentSellingPriceBeforeVat { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public decimal? SuggestedMarkdownPriceBeforeVat { get; set; }

    public decimal? SuggestedMarkdownPriceAfterVat { get; set; }

    public decimal? MarkdownDepthPercent { get; set; }

    public decimal? EstimatedInventoryValue { get; set; }

    public decimal? EstimatedRecoveryValue { get; set; }

    public string Severity { get; set; } = "warning";

    public string RuleCode { get; set; } = "markdown_candidate";

    public string RecommendedActionCode { get; set; } = string.Empty;

    public Dictionary<string, string> RecommendationParams { get; set; } = new();

    public InsightRecommendationDto Recommendation { get; set; } = new();
}

public class PromotionRiskInsightDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }

    public decimal QuantityAvailable { get; set; }

    public decimal OutboundQuantity { get; set; }

    public decimal AverageDailyOutbound { get; set; }

    public decimal? EstimatedDaysOfCover { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? RegularPriceBeforeVat { get; set; }

    public decimal? RegularPriceAfterVat { get; set; }

    public decimal? PromotionPriceBeforeVat { get; set; }

    public decimal? PromotionPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public decimal? PromotionDiscountPercent { get; set; }

    public decimal? MarginRateAfterPromotion { get; set; }

    public string Severity { get; set; } = "warning";

    public string RuleCode { get; set; } = "promotion_risk";

    public string RecommendedActionCode { get; set; } = string.Empty;

    public Dictionary<string, string> RecommendationParams { get; set; } = new();

    public InsightRecommendationDto Recommendation { get; set; } = new();
}

public class ReorderRiskInsightDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }

    public decimal QuantityAvailable { get; set; }

    public decimal QuantityOnOrder { get; set; }

    public decimal QuantityInReceiving { get; set; }

    public decimal AverageDailyOutbound { get; set; }

    public decimal? EstimatedDaysOfCover { get; set; }

    public decimal? ReorderPoint { get; set; }

    public decimal? SuggestedReorderQuantity { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public string Severity { get; set; } = "warning";

    public string RuleCode { get; set; } = "reorder_risk";

    public string RecommendedActionCode { get; set; } = string.Empty;

    public Dictionary<string, string> RecommendationParams { get; set; } = new();

    public InsightRecommendationDto Recommendation { get; set; } = new();
}

public class TrendSummaryInsightDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }

    public decimal CurrentQuantityOnHand { get; set; }

    public decimal CurrentInventoryValue { get; set; }

    public decimal PreviousInventoryValue { get; set; }

    public decimal InventoryValueDelta { get; set; }

    public decimal CurrentAverageDailyOutbound { get; set; }

    public decimal PreviousAverageDailyOutbound { get; set; }

    public decimal OutboundTrendPercent { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? PreviousSellingPriceAfterVat { get; set; }

    public decimal? PriceTrendPercent { get; set; }

    public string Severity { get; set; } = "info";

    public string RuleCode { get; set; } = "trend_summary";

    public string RecommendedActionCode { get; set; } = string.Empty;

    public Dictionary<string, string> RecommendationParams { get; set; } = new();

    public InsightRecommendationDto Recommendation { get; set; } = new();
}

public class InsightsExecutiveSummaryDto
{
    public int DeadStockCount { get; set; }

    public decimal TiedCapital { get; set; }

    public decimal InventoryValueAtRisk { get; set; }

    public decimal MarginAtRisk { get; set; }

    public int PromotionRiskCount { get; set; }

    public int ReorderRiskCount { get; set; }

    public int TransferOpportunityCount { get; set; }

    public decimal TransferOpportunityValue { get; set; }

    public int MarkdownCandidateCount { get; set; }

    public decimal MarkdownRecoveryValue { get; set; }
}
