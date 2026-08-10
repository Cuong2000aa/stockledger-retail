using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Application.Insights;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Insights;
using Xunit;

namespace StockLedgerRetail.Application.Tests;

public class TransferRebalanceEngineTests
{
    private static readonly Guid SkuId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WhSource = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid WhDestLowCover = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid WhDestHighCover = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid BrandA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BrandB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Heuristic_prefers_lower_cover_destination_when_surplus_is_limited()
    {
        var engine = CreateEngine(TransferRebalanceMode.Heuristic);
        var facts = new List<SalesVelocityFact>
        {
            Fact(WhSource, BrandA, "HN", available: 20, outbound: 0, cost: 10, sell: 20),
            // cover = 10/2 = 5 days, need = 2*14 - 10 = 18
            Fact(WhDestLowCover, BrandA, "HN", available: 10, outbound: 60, cost: 10, sell: 25),
            // cover = 40/1 = 40 days, need = 0 (above target) — use smaller outbound so still needs a little
            // avg=1, desired=14, available=12 → need=2, cover=12
            Fact(WhDestHighCover, BrandA, "HN", available: 12, outbound: 30, cost: 10, sell: 22)
        };

        var result = engine.Suggest(facts, Array.Empty<TransferPolicy>(), DefaultRequest(maxResults: 10));

        Assert.NotEmpty(result);
        var toLow = result.Single(x => x.DestinationWarehouseId == WhDestLowCover);
        var toHigh = result.SingleOrDefault(x => x.DestinationWarehouseId == WhDestHighCover);

        // Limited surplus 20: low-cover need 18 should be filled before high-cover need 2.
        Assert.Equal(18, toLow.SuggestedQuantity);
        Assert.NotNull(toHigh);
        Assert.Equal(2, toHigh!.SuggestedQuantity);
        Assert.Equal(TransferRebalanceEngine.RuleCodeV2, toLow.RuleCode);
    }

    [Fact]
    public void Heuristic_blocks_cross_brand_without_policy()
    {
        var engine = CreateEngine(TransferRebalanceMode.Heuristic);
        var facts = new List<SalesVelocityFact>
        {
            Fact(WhSource, BrandA, "HN", available: 50, outbound: 0, cost: 10, sell: 20),
            Fact(WhDestLowCover, BrandB, "HN", available: 5, outbound: 60, cost: 10, sell: 25)
        };

        var result = engine.Suggest(facts, Array.Empty<TransferPolicy>(), DefaultRequest());

        Assert.Empty(result);
    }

    [Fact]
    public void Heuristic_allows_cross_brand_with_active_policy()
    {
        var engine = CreateEngine(TransferRebalanceMode.Heuristic);
        var facts = new List<SalesVelocityFact>
        {
            Fact(WhSource, BrandA, "HN", available: 50, outbound: 0, cost: 10, sell: 20),
            Fact(WhDestLowCover, BrandB, "HN", available: 5, outbound: 60, cost: 10, sell: 25)
        };
        var policies = new List<TransferPolicy>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SourceBrandId = BrandA,
                DestinationBrandId = BrandB,
                AllowCrossBrand = true,
                IsActive = true
            }
        };

        var result = engine.Suggest(facts, policies, DefaultRequest());

        Assert.Single(result);
        Assert.Equal(WhSource, result[0].SourceWarehouseId);
        Assert.Equal(WhDestLowCover, result[0].DestinationWarehouseId);
        Assert.True(result[0].SuggestedQuantity > 0);
    }

    [Fact]
    public void Heuristic_respects_surplus_and_need_residuals()
    {
        var engine = CreateEngine(TransferRebalanceMode.Heuristic);
        var facts = BuildGoldenFacts();

        var result = engine.Suggest(facts, Array.Empty<TransferPolicy>(), DefaultRequest(maxResults: 50));

        var allocated = result.Sum(x => x.SuggestedQuantity);
        // Source surplus: available 100, outbound 0 → surplus 100
        Assert.True(allocated <= 100m);
        // Dest needs: low cover need=18, mid need roughly from facts
        var needByDest = result.GroupBy(x => x.DestinationWarehouseId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SuggestedQuantity));
        foreach (var qty in needByDest.Values)
        {
            Assert.True(qty > 0);
        }
    }

    [Fact]
    public void Heuristic_golden_quantities_match_fixture()
    {
        var engine = CreateEngine(TransferRebalanceMode.Heuristic);
        var facts = BuildGoldenFacts();

        var result = engine.Suggest(facts, Array.Empty<TransferPolicy>(), DefaultRequest(maxResults: 50));

        // Expected: source surplus 100.
        // DestLow: avg=2, need=18, cover=5 → filled first with 18
        // DestHigh: avg=1, need=2, cover=12 → filled with 2
        Assert.Equal(2, result.Count);
        Assert.Equal(18, result.Single(x => x.DestinationWarehouseId == WhDestLowCover).SuggestedQuantity);
        Assert.Equal(2, result.Single(x => x.DestinationWarehouseId == WhDestHighCover).SuggestedQuantity);
        Assert.Equal(20, result.Sum(x => x.SuggestedQuantity));
    }

    [Fact]
    public void MinCostFlow_matches_or_beats_heuristic_on_margin_and_shortfall()
    {
        var facts = BuildComparisonFacts();
        var request = DefaultRequest(maxResults: 50);

        var heuristic = CreateEngine(TransferRebalanceMode.Heuristic)
            .Suggest(facts, Array.Empty<TransferPolicy>(), request);
        var minCost = CreateEngine(TransferRebalanceMode.MinCostFlow)
            .Suggest(facts, Array.Empty<TransferPolicy>(), request);

        Assert.NotEmpty(heuristic);
        Assert.NotEmpty(minCost);

        var hMargin = heuristic.Sum(x => x.MarginOpportunity ?? 0);
        var mMargin = minCost.Sum(x => x.MarginOpportunity ?? 0);
        var hShortfall = RemainingShortfall(facts, heuristic, lookback: 30, targetCover: 14);
        var mShortfall = RemainingShortfall(facts, minCost, lookback: 30, targetCover: 14);

        // Min-cost should not leave more unmet need than heuristic, and margin should be at least as good
        // within a small tolerance for scaling.
        Assert.True(mShortfall <= hShortfall + 0.01m);
        Assert.True(mMargin + 0.01m >= hMargin * 0.95m);
    }

    [Fact]
    public void MinCostFlow_falls_back_when_graph_too_large()
    {
        var options = Options.Create(new TransferRebalanceOptions
        {
            Mode = TransferRebalanceMode.MinCostFlow,
            MaxEdgesPerSku = 0
        });
        var engine = new TransferRebalanceEngine(options, NullLogger<TransferRebalanceEngine>.Instance);
        var facts = BuildGoldenFacts();

        var result = engine.Suggest(facts, Array.Empty<TransferPolicy>(), DefaultRequest());

        Assert.NotEmpty(result);
        Assert.All(result, x => Assert.Equal(TransferRebalanceEngine.RuleCodeV2, x.RuleCode));
    }

    private static decimal RemainingShortfall(
        IReadOnlyList<SalesVelocityFact> facts,
        IReadOnlyList<TransferSuggestionDto> suggestions,
        int lookback,
        int targetCover)
    {
        var received = suggestions
            .GroupBy(x => (x.ProductVariantId, x.DestinationWarehouseId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SuggestedQuantity));

        decimal shortfall = 0;
        foreach (var fact in facts)
        {
            var avg = fact.OutboundQuantity / lookback;
            if (avg <= 0)
            {
                continue;
            }

            var need = Math.Max(0, avg * targetCover - fact.QuantityAvailable);
            received.TryGetValue((fact.ProductVariantId, fact.WarehouseId), out var got);
            shortfall += Math.Max(0, need - got);
        }

        return shortfall;
    }

    private static List<SalesVelocityFact> BuildGoldenFacts() =>
    [
        Fact(WhSource, BrandA, "HN", available: 100, outbound: 0, cost: 10, sell: 20),
        Fact(WhDestLowCover, BrandA, "HN", available: 10, outbound: 60, cost: 10, sell: 25),
        Fact(WhDestHighCover, BrandA, "HN", available: 12, outbound: 30, cost: 10, sell: 22)
    ];

    /// <summary>
    /// Two sources with different margins so MinCostFlow and Heuristic can diverge meaningfully.
    /// </summary>
    private static List<SalesVelocityFact> BuildComparisonFacts()
    {
        var whCheap = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var whPremium = Guid.Parse("66666666-6666-6666-6666-666666666666");
        return
        [
            // Cheap source: high surplus, higher cost → lower margin
            Fact(whCheap, BrandA, "HN", available: 80, outbound: 0, cost: 18, sell: 20, sku: "SKU-CMP"),
            // Premium source: less surplus, lower cost → higher margin
            Fact(whPremium, BrandA, "HN", available: 40, outbound: 0, cost: 8, sell: 20, sku: "SKU-CMP"),
            // Dest urgently needs ~28 units (avg 2 * 14 - 0)
            Fact(WhDestLowCover, BrandA, "HN", available: 0, outbound: 60, cost: 10, sell: 30, sku: "SKU-CMP"),
            // Second dest needs ~14
            Fact(WhDestHighCover, BrandA, "HN", available: 0, outbound: 30, cost: 10, sell: 28, sku: "SKU-CMP")
        ];
    }

    private static SalesVelocityFact Fact(
        Guid warehouseId,
        Guid brandId,
        string region,
        decimal available,
        decimal outbound,
        decimal cost,
        decimal sell,
        string sku = "SKU-1") =>
        new()
        {
            ProductVariantId = SkuId,
            Sku = sku,
            WarehouseId = warehouseId,
            BrandId = brandId,
            RegionCode = region,
            WarehouseType = WarehouseType.Store,
            WarehouseCode = warehouseId.ToString()[..8],
            WarehouseName = warehouseId.ToString()[..8],
            QuantityOnHand = available,
            QuantityAvailable = available,
            OutboundQuantity = outbound,
            CostPrice = cost,
            CurrentSellingPriceBeforeVat = sell,
            CurrentSellingPriceAfterVat = sell * 1.1m,
            VatRate = 10
        };

    private static TransferRebalanceRequest DefaultRequest(int maxResults = 20) =>
        new()
        {
            LookbackDays = 30,
            TargetCoverDays = 14,
            ReserveCoverDays = 7,
            MaxResults = maxResults
        };

    private static TransferRebalanceEngine CreateEngine(TransferRebalanceMode mode) =>
        new(
            Options.Create(new TransferRebalanceOptions { Mode = mode }),
            NullLogger<TransferRebalanceEngine>.Instance);
}
