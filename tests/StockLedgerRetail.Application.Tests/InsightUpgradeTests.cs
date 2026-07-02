using StockLedgerRetail.Application.Insights;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;
using Xunit;

namespace StockLedgerRetail.Application.Tests;

public class FashionInsightAnalyzerTests
{
    [Fact]
    public void BuildBrokenSizeRuns_detects_incomplete_size_curve()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var facts = new List<FashionStockFact>
        {
            CreateFact(productId, warehouseId, "S", 5),
            CreateFact(productId, warehouseId, "M", 0),
            CreateFact(productId, warehouseId, "L", 0)
        };

        var runs = FashionInsightAnalyzer.BuildBrokenSizeRuns(facts, 10);

        Assert.Single(runs);
        Assert.Equal(1, runs[0].SizesWithStock);
        Assert.Equal(2, runs[0].SizesWithoutStock);
        Assert.Contains("M", runs[0].MissingSizes);
    }

    [Fact]
    public void BuildSeasonClearance_flags_past_season_idle_stock()
    {
        var facts = new List<FashionStockFact>
        {
            new()
            {
                ProductVariantId = Guid.NewGuid(),
                Sku = "SKU-SS23",
                ProductName = "Dress",
                Season = "SS23",
                WarehouseId = Guid.NewGuid(),
                WarehouseCode = "WH01",
                WarehouseName = "Store 1",
                QuantityOnHand = 12,
                OutboundQuantity = 0,
                LastOutboundAt = DateTime.UtcNow.AddDays(-90)
            }
        };

        var items = FashionInsightAnalyzer.BuildSeasonClearance(facts, "FW24", 60, 10);

        Assert.Single(items);
        Assert.Equal("critical", items[0].Severity);
    }

    private static FashionStockFact CreateFact(Guid productId, Guid warehouseId, string size, decimal qty) =>
        new()
        {
            ProductId = productId,
            ProductName = "Shirt",
            Color = "Black",
            Size = size,
            WarehouseId = warehouseId,
            WarehouseCode = "WH01",
            WarehouseName = "Store 1",
            ProductVariantId = Guid.NewGuid(),
            Sku = $"SKU-{size}",
            QuantityOnHand = qty,
            OutboundQuantity = 1
        };
}

public class MarkdownWhatIfServiceTests
{
    [Fact]
    public void Simulate_calculates_recovery_and_margin()
    {
        var service = new MarkdownWhatIfService();
        var variantId = Guid.NewGuid();

        var result = service.Simulate(new MarkdownWhatIfRequestDto
        {
            VatRate = 10,
            Lines =
            [
                new MarkdownWhatIfLineDto
                {
                    ProductVariantId = variantId,
                    Sku = "SKU-1",
                    WarehouseId = Guid.NewGuid(),
                    QuantityOnHand = 10,
                    RegularPriceBeforeVat = 100_000,
                    CostPrice = 50_000,
                    MarkdownPercent = 20
                }
            ]
        });

        Assert.Single(result.Lines);
        Assert.Equal(80_000, result.Lines[0].PriceBeforeVatAfterMarkdown);
        Assert.True(result.TotalRecoveryValueAfterVat > 0);
        Assert.Equal(37.5m, result.Lines[0].GrossMarginPercentAfterMarkdown);
    }
}

public class InsightExplainServiceTests
{
    [Fact]
    public void Explain_returns_rationale_for_transfer_action()
    {
        var service = new InsightExplainService();

        var response = service.Explain(new InsightExplainRequestDto
        {
            InsightKind = "transfer",
            ActionCode = InsightActionCodes.TransferExecute,
            Sku = "SKU-1",
            SourceWarehouseCode = "WH-A",
            DestinationWarehouseCode = "WH-B",
            Priority = 75,
            Evidence = new Dictionary<string, string> { ["quantity"] = "5" }
        });

        Assert.Contains("transfer", response.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(response.SuggestedNextSteps);
    }
}
