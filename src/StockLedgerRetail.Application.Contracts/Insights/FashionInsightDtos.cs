namespace StockLedgerRetail.Insights;

public class BrokenSizeRunInsightDto
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? Color { get; set; }

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }

    public int TotalSizesInRun { get; set; }

    public int SizesWithStock { get; set; }

    public int SizesWithoutStock { get; set; }

    public List<string> SizesInStock { get; set; } = new();

    public List<string> MissingSizes { get; set; } = new();

    public List<BrokenSizeRunVariantDto> Variants { get; set; } = new();

    public decimal TotalOnHand { get; set; }

    public decimal? InventoryValue { get; set; }

    public string Severity { get; set; } = "warning";

    public string RuleCode { get; set; } = "broken_size_run";

    public InsightRecommendationDto Recommendation { get; set; } = new();
}

public class BrokenSizeRunVariantDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Size { get; set; }

    public decimal QuantityOnHand { get; set; }
}

public class SeasonClearanceInsightDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? Season { get; set; }

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal OutboundQuantity { get; set; }

    public int DaysWithoutOutbound { get; set; }

    public decimal? InventoryValue { get; set; }

    public decimal? SuggestedMarkdownPriceAfterVat { get; set; }

    public decimal? MarkdownDepthPercent { get; set; }

    public string Severity { get; set; } = "warning";

    public string RuleCode { get; set; } = "season_clearance";

    public InsightRecommendationDto Recommendation { get; set; } = new();
}
