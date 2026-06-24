namespace StockLedgerRetail.Insights;

public class InsightRecommendationDto
{
    public string ActionCode { get; set; } = string.Empty;

    public string ActionType { get; set; } = InsightActionTypes.Review;

    public string TitleKey { get; set; } = string.Empty;

    public int Priority { get; set; }

    public Dictionary<string, string> Params { get; set; } = new();

    public Dictionary<string, string> Evidence { get; set; } = new();

    public List<InsightRecommendationCtaDto> Actions { get; set; } = [];
}

public class InsightRecommendationCtaDto
{
    public string Id { get; set; } = string.Empty;

    public string LabelKey { get; set; } = string.Empty;

    public string Kind { get; set; } = InsightCtaKinds.Navigate;

    public string? Route { get; set; }

    public string? ApiOperation { get; set; }

    public bool IsPrimary { get; set; }

    public Dictionary<string, string> Payload { get; set; } = new();
}
