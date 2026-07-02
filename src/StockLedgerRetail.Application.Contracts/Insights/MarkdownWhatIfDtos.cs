namespace StockLedgerRetail.Insights;

public class MarkdownWhatIfLineDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal? RegularPriceBeforeVat { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal MarkdownPercent { get; set; }
}

public class MarkdownWhatIfRequestDto
{
    public List<MarkdownWhatIfLineDto> Lines { get; set; } = new();

    public decimal? VatRate { get; set; }
}

public class MarkdownWhatIfLineResultDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal MarkdownPercent { get; set; }

    public decimal? PriceBeforeVatAfterMarkdown { get; set; }

    public decimal? PriceAfterVatAfterMarkdown { get; set; }

    public decimal? RecoveryValueAfterVat { get; set; }

    public decimal? GrossMarginPercentAfterMarkdown { get; set; }

    public decimal? InventoryValueAtCost { get; set; }
}

public class MarkdownWhatIfResultDto
{
    public List<MarkdownWhatIfLineResultDto> Lines { get; set; } = new();

    public decimal TotalRecoveryValueAfterVat { get; set; }

    public decimal TotalInventoryValueAtCost { get; set; }

    public decimal CapitalReleasePercent { get; set; }
}
