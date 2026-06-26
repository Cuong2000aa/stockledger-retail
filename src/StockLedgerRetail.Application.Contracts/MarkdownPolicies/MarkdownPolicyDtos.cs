using StockLedgerRetail.Enums;

namespace StockLedgerRetail.MarkdownPolicies;

public class MarkdownPolicyTierDto
{
    public string TierCode { get; set; } = "light";

    public int MinDaysWithoutOutbound { get; set; }

    public int? MaxDaysWithoutOutbound { get; set; }

    public decimal MarkdownPercent { get; set; }

    public decimal? SlowSellThroughMarkdownPercent { get; set; }

    public string Severity { get; set; } = "warning";
}

public class MarkdownPolicyDto
{
    public Guid Id { get; set; }

    public Guid BrandId { get; set; }

    public string? BrandName { get; set; }

    public string? RegionCode { get; set; }

    public WarehouseType? WarehouseType { get; set; }

    public int LookbackDays { get; set; }

    public int MinDaysWithoutOutbound { get; set; }

    public decimal MinOnHand { get; set; }

    public decimal? MinInventoryValueAtCost { get; set; }

    public decimal MinGrossMarginPercent { get; set; }

    public decimal MaxMarkdownPercent { get; set; }

    public bool AllowBelowCost { get; set; }

    public decimal? RequireApprovalAbovePercent { get; set; }

    public decimal SlowSellThroughThreshold { get; set; }

    public List<MarkdownPolicyTierDto> Tiers { get; set; } = [];

    public bool IsActive { get; set; }

    public string? Note { get; set; }
}

public class CreateMarkdownPolicyDto
{
    public Guid BrandId { get; set; }

    public string? RegionCode { get; set; }

    public WarehouseType? WarehouseType { get; set; }

    public int LookbackDays { get; set; } = 30;

    public int MinDaysWithoutOutbound { get; set; } = 60;

    public decimal MinOnHand { get; set; } = 1;

    public decimal? MinInventoryValueAtCost { get; set; }

    public decimal MinGrossMarginPercent { get; set; } = 10;

    public decimal MaxMarkdownPercent { get; set; } = 50;

    public bool AllowBelowCost { get; set; }

    public decimal? RequireApprovalAbovePercent { get; set; }

    public decimal SlowSellThroughThreshold { get; set; } = 0.5m;

    public List<MarkdownPolicyTierDto> Tiers { get; set; } = [];

    public string? Note { get; set; }
}

public class UpdateMarkdownPolicyDto
{
    public string? RegionCode { get; set; }

    public WarehouseType? WarehouseType { get; set; }

    public int LookbackDays { get; set; } = 30;

    public int MinDaysWithoutOutbound { get; set; } = 60;

    public decimal MinOnHand { get; set; } = 1;

    public decimal? MinInventoryValueAtCost { get; set; }

    public decimal MinGrossMarginPercent { get; set; } = 10;

    public decimal MaxMarkdownPercent { get; set; } = 50;

    public bool AllowBelowCost { get; set; }

    public decimal? RequireApprovalAbovePercent { get; set; }

    public decimal SlowSellThroughThreshold { get; set; } = 0.5m;

    public List<MarkdownPolicyTierDto> Tiers { get; set; } = [];

    public bool IsActive { get; set; }

    public string? Note { get; set; }
}

public class MarkdownSuggestionDto
{
    public Guid? PolicyId { get; set; }

    public string TierCode { get; set; } = string.Empty;

    public decimal MarkdownDepthPercent { get; set; }

    public decimal? SuggestedMarkdownPriceBeforeVat { get; set; }

    public decimal? SuggestedMarkdownPriceAfterVat { get; set; }

    public decimal? GrossMarginAfterMarkdownPercent { get; set; }

    public decimal? ShopSellThroughRatio { get; set; }

    public decimal? BrandMedianSellThroughRatio { get; set; }

    public string Severity { get; set; } = "warning";

    public string RuleCode { get; set; } = "markdown_policy";

    public bool RequiresApproval { get; set; }
}
