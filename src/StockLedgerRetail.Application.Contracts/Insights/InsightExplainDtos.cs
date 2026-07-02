namespace StockLedgerRetail.Insights;

public class InsightExplainRequestDto
{
    public string InsightKind { get; set; } = string.Empty;

    public string ActionCode { get; set; } = string.Empty;

    public string? Sku { get; set; }

    public string? WarehouseCode { get; set; }

    public string? SourceWarehouseCode { get; set; }

    public string? DestinationWarehouseCode { get; set; }

    public int Priority { get; set; }

    public Dictionary<string, string> Evidence { get; set; } = new();

    public Dictionary<string, string> Params { get; set; } = new();
}

public class InsightExplainResponseDto
{
    public string Summary { get; set; } = string.Empty;

    public List<string> RationaleParagraphs { get; set; } = new();

    public List<string> EvidenceLines { get; set; } = new();

    public List<string> SuggestedNextSteps { get; set; } = new();
}
