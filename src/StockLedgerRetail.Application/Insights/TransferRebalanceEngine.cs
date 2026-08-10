using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Application.Insights;

/// <summary>
/// Transfer suggestion optimizer (Heuristic scored multi-pass + optional MinCostFlow).
/// Algorithm notes: docs/TransferRebalance.vi.md (canonical) and docs/TransferRebalance.md.
/// </summary>
public class TransferRebalanceEngine : ITransferRebalanceEngine
{
    public const string RuleCodeV2 = "transfer_rebalance_v2";

    private readonly TransferRebalanceOptions _options;
    private readonly ILogger<TransferRebalanceEngine> _logger;

    public TransferRebalanceEngine(
        IOptions<TransferRebalanceOptions> options,
        ILogger<TransferRebalanceEngine> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public List<TransferSuggestionDto> Suggest(
        IReadOnlyList<SalesVelocityFact> facts,
        IReadOnlyList<TransferPolicy> policies,
        TransferRebalanceRequest request)
    {
        var lookbackDays = NormalizePositive(request.LookbackDays, 30);
        var targetCoverDays = NormalizePositive(request.TargetCoverDays, 14, 90);
        var reserveCoverDays = NormalizePositive(request.ReserveCoverDays, 7, 60);
        var maxResults = NormalizePositive(request.MaxResults, 20, 100);

        var suggestions = new List<TransferSuggestionDto>();

        foreach (var skuGroup in facts.GroupBy(x => x.ProductVariantId))
        {
            var nodes = BuildNodes(
                skuGroup.ToList(),
                request.SourceWarehouseId,
                request.DestinationWarehouseId,
                lookbackDays,
                targetCoverDays,
                reserveCoverDays);

            if (nodes.Sources.Count == 0 || nodes.Destinations.Count == 0)
            {
                continue;
            }

            var edges = BuildEdges(nodes, skuGroup.First().BrandId, policies);
            if (edges.Count == 0)
            {
                continue;
            }

            List<Allocation> allocations;
            if (_options.Mode == TransferRebalanceMode.MinCostFlow
                && edges.Count <= _options.MaxEdgesPerSku)
            {
                allocations = TryMinCostFlow(nodes, edges) ?? AllocateHeuristic(nodes, edges);
            }
            else
            {
                if (_options.Mode == TransferRebalanceMode.MinCostFlow
                    && edges.Count > _options.MaxEdgesPerSku)
                {
                    _logger.LogDebug(
                        "MinCostFlow skipped for SKU {Sku}: {EdgeCount} edges > MaxEdgesPerSku {Max}.",
                        skuGroup.First().Sku,
                        edges.Count,
                        _options.MaxEdgesPerSku);
                }

                allocations = AllocateHeuristic(nodes, edges);
            }

            foreach (var allocation in allocations)
            {
                suggestions.Add(ToDto(allocation, targetCoverDays));
            }
        }

        return suggestions
            .OrderByDescending(ScoreForSort)
            .ThenByDescending(x => x.SuggestedQuantity)
            .ThenBy(x => x.DestinationDaysOfCover ?? decimal.MaxValue)
            .Take(maxResults)
            .ToList();
    }

    private List<Allocation>? TryMinCostFlow(SkuNodes nodes, List<CandidateEdge> edges)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var result = TransferMinCostFlowSolver.Solve(nodes, edges, _options);
            sw.Stop();
            if (result is null)
            {
                _logger.LogDebug("MinCostFlow returned null; falling back to Heuristic.");
                return null;
            }

            if (sw.ElapsedMilliseconds > _options.MinCostFlowTimeoutMs)
            {
                _logger.LogDebug(
                    "MinCostFlow exceeded timeout ({Elapsed}ms > {Timeout}ms); falling back to Heuristic.",
                    sw.ElapsedMilliseconds,
                    _options.MinCostFlowTimeoutMs);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MinCostFlow failed; falling back to Heuristic.");
            return null;
        }
    }

    internal List<Allocation> AllocateHeuristic(SkuNodes nodes, List<CandidateEdge> edges)
    {
        var surplus = nodes.Sources.ToDictionary(s => s.WarehouseId, s => s.Surplus);
        var need = nodes.Destinations.ToDictionary(d => d.WarehouseId, d => d.Need);

        // Score with initial feasible qty, then multi-pass allocate in score order updating residuals.
        var ranked = edges
            .Select(e =>
            {
                var qty = Math.Min(surplus[e.Source.WarehouseId], need[e.Destination.WarehouseId]);
                return (Edge: e, Score: ScoreEdge(e, qty));
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Edge.Destination.Need)
            .ThenBy(x => x.Edge.Destination.CoverDays ?? decimal.MaxValue)
            .Select(x => x.Edge)
            .ToList();

        var allocations = new List<Allocation>();
        foreach (var edge in ranked)
        {
            var available = surplus[edge.Source.WarehouseId];
            var required = need[edge.Destination.WarehouseId];
            var qty = Math.Min(available, required);
            if (qty <= 0)
            {
                continue;
            }

            surplus[edge.Source.WarehouseId] = available - qty;
            need[edge.Destination.WarehouseId] = required - qty;
            allocations.Add(new Allocation(edge, qty));
        }

        return MergeAllocations(allocations);
    }

    private decimal ScoreEdge(CandidateEdge edge, decimal qty)
    {
        if (qty <= 0)
        {
            qty = Math.Min(edge.Source.Surplus, edge.Destination.Need);
        }

        var coverGap = edge.Destination.CoverDays.HasValue
            ? Math.Max(0, edge.TargetCoverDays - edge.Destination.CoverDays.Value)
            : edge.TargetCoverDays;

        var marginPerUnit = 0m;
        if (edge.Destination.Fact.CurrentSellingPriceBeforeVat.HasValue && edge.Source.Fact.CostPrice.HasValue)
        {
            marginPerUnit = edge.Destination.Fact.CurrentSellingPriceBeforeVat.Value - edge.Source.Fact.CostPrice.Value;
        }

        var marginOpportunity = marginPerUnit * qty;
        var regionPenalty = RegionsMatch(edge.Source.Fact.RegionCode, edge.Destination.Fact.RegionCode)
            ? 0m
            : _options.CrossRegionPenalty;

        return _options.CoverGapWeight * coverGap
            + _options.MarginWeight * marginOpportunity
            + _options.QuantityWeight * qty
            - regionPenalty;
    }

    internal static decimal EdgeCostForMinCostFlow(CandidateEdge edge, TransferRebalanceOptions options)
    {
        // Lower cost = better. Prefer low cover days and high margin.
        var coverComponent = edge.Destination.CoverDays ?? edge.TargetCoverDays;
        var marginPerUnit = 0m;
        if (edge.Destination.Fact.CurrentSellingPriceBeforeVat.HasValue && edge.Source.Fact.CostPrice.HasValue)
        {
            marginPerUnit = edge.Destination.Fact.CurrentSellingPriceBeforeVat.Value - edge.Source.Fact.CostPrice.Value;
        }

        var regionPenalty = RegionsMatch(edge.Source.Fact.RegionCode, edge.Destination.Fact.RegionCode)
            ? 0m
            : options.CrossRegionPenalty;

        // Scale margin so cover days dominate mild differences; negative margin increases cost.
        return coverComponent * options.CoverGapWeight
            - marginPerUnit * options.MarginWeight
            + regionPenalty;
    }

    private static List<Allocation> MergeAllocations(List<Allocation> allocations)
    {
        return allocations
            .GroupBy(a => (a.Edge.Source.WarehouseId, a.Edge.Destination.WarehouseId))
            .Select(g => new Allocation(g.First().Edge, g.Sum(x => x.Quantity)))
            .Where(a => a.Quantity > 0)
            .ToList();
    }

    private static SkuNodes BuildNodes(
        IReadOnlyList<SalesVelocityFact> skuFacts,
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        int lookbackDays,
        int targetCoverDays,
        int reserveCoverDays)
    {
        var sources = skuFacts
            .Where(x => !sourceWarehouseId.HasValue || x.WarehouseId == sourceWarehouseId.Value)
            .Select(x =>
            {
                var avgDaily = x.OutboundQuantity / lookbackDays;
                var surplus = Math.Max(
                    0,
                    x.QuantityAvailable - (x.OutboundQuantity > 0 ? avgDaily * reserveCoverDays : 0));
                return new SourceNode(x, surplus, avgDaily);
            })
            .Where(x => x.Surplus > 0)
            .ToList();

        var destinations = skuFacts
            .Where(x => !destinationWarehouseId.HasValue || x.WarehouseId == destinationWarehouseId.Value)
            .Select(x =>
            {
                var avgDaily = x.OutboundQuantity / lookbackDays;
                var desired = avgDaily * targetCoverDays;
                var need = Math.Max(0, desired - x.QuantityAvailable);
                var coverDays = avgDaily > 0 ? x.QuantityAvailable / avgDaily : (decimal?)null;
                return new DestinationNode(x, need, avgDaily, coverDays, targetCoverDays);
            })
            .Where(x => x.AverageDailyOutbound > 0 && x.Need > 0)
            .ToList();

        return new SkuNodes(sources, destinations);
    }

    private static List<CandidateEdge> BuildEdges(
        SkuNodes nodes,
        Guid? productBrandId,
        IReadOnlyList<TransferPolicy> policies)
    {
        var edges = new List<CandidateEdge>();
        foreach (var source in nodes.Sources)
        {
            foreach (var dest in nodes.Destinations)
            {
                if (source.WarehouseId == dest.WarehouseId)
                {
                    continue;
                }

                if (!InsightTransferRules.CanTransferBetweenWarehouses(
                        source.Fact.BrandId,
                        dest.Fact.BrandId,
                        productBrandId,
                        source.Fact.RegionCode,
                        dest.Fact.RegionCode,
                        policies))
                {
                    continue;
                }

                edges.Add(new CandidateEdge(source, dest, dest.TargetCoverDays));
            }
        }

        return edges;
    }

    private static TransferSuggestionDto ToDto(Allocation allocation, int targetCoverDays)
    {
        var source = allocation.Edge.Source;
        var dest = allocation.Edge.Destination;
        var qty = allocation.Quantity;

        return new TransferSuggestionDto
        {
            ProductVariantId = dest.Fact.ProductVariantId,
            Sku = dest.Fact.Sku,
            SourceWarehouseId = source.WarehouseId,
            SourceWarehouseCode = source.Fact.WarehouseCode,
            SourceWarehouseName = source.Fact.WarehouseName,
            DestinationWarehouseId = dest.WarehouseId,
            DestinationWarehouseCode = dest.Fact.WarehouseCode,
            DestinationWarehouseName = dest.Fact.WarehouseName,
            SuggestedQuantity = qty,
            SourceAvailable = source.Fact.QuantityAvailable,
            DestinationAvailable = dest.Fact.QuantityAvailable,
            DestinationAverageDailyOutbound = dest.AverageDailyOutbound,
            DestinationDaysOfCover = dest.CoverDays,
            SourceCostPrice = source.Fact.CostPrice,
            CurrentSellingPriceBeforeVat = dest.Fact.CurrentSellingPriceBeforeVat,
            CurrentSellingPriceAfterVat = dest.Fact.CurrentSellingPriceAfterVat,
            VatRate = dest.Fact.VatRate,
            TransferValue = source.Fact.CostPrice.HasValue ? source.Fact.CostPrice.Value * qty : null,
            MarginOpportunity = dest.Fact.CurrentSellingPriceBeforeVat.HasValue && source.Fact.CostPrice.HasValue
                ? (dest.Fact.CurrentSellingPriceBeforeVat.Value - source.Fact.CostPrice.Value) * qty
                : null,
            Severity = dest.CoverDays.HasValue && dest.CoverDays.Value < 7 ? "critical" : "warning",
            RuleCode = RuleCodeV2
        };
    }

    private static decimal ScoreForSort(TransferSuggestionDto x)
    {
        var coverBoost = x.DestinationDaysOfCover.HasValue
            ? Math.Max(0, 14 - x.DestinationDaysOfCover.Value)
            : 14;
        return coverBoost * 3 + (x.MarginOpportunity ?? 0) + x.SuggestedQuantity * 0.1m;
    }

    private static bool RegionsMatch(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return true;
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static int NormalizePositive(int value, int fallback, int? max = null)
    {
        var normalized = value <= 0 ? fallback : value;
        return max.HasValue ? Math.Min(normalized, max.Value) : normalized;
    }

    internal sealed class SourceNode
    {
        public SourceNode(SalesVelocityFact fact, decimal surplus, decimal averageDailyOutbound)
        {
            Fact = fact;
            Surplus = surplus;
            AverageDailyOutbound = averageDailyOutbound;
        }

        public SalesVelocityFact Fact { get; }
        public Guid WarehouseId => Fact.WarehouseId;
        public decimal Surplus { get; }
        public decimal AverageDailyOutbound { get; }
    }

    internal sealed class DestinationNode
    {
        public DestinationNode(
            SalesVelocityFact fact,
            decimal need,
            decimal averageDailyOutbound,
            decimal? coverDays,
            int targetCoverDays)
        {
            Fact = fact;
            Need = need;
            AverageDailyOutbound = averageDailyOutbound;
            CoverDays = coverDays;
            TargetCoverDays = targetCoverDays;
        }

        public SalesVelocityFact Fact { get; }
        public Guid WarehouseId => Fact.WarehouseId;
        public decimal Need { get; }
        public decimal AverageDailyOutbound { get; }
        public decimal? CoverDays { get; }
        public int TargetCoverDays { get; }
    }

    internal sealed class CandidateEdge
    {
        public CandidateEdge(SourceNode source, DestinationNode destination, int targetCoverDays)
        {
            Source = source;
            Destination = destination;
            TargetCoverDays = targetCoverDays;
        }

        public SourceNode Source { get; }
        public DestinationNode Destination { get; }
        public int TargetCoverDays { get; }
    }

    internal sealed class Allocation
    {
        public Allocation(CandidateEdge edge, decimal quantity)
        {
            Edge = edge;
            Quantity = quantity;
        }

        public CandidateEdge Edge { get; }
        public decimal Quantity { get; }
    }

    internal sealed class SkuNodes
    {
        public SkuNodes(List<SourceNode> sources, List<DestinationNode> destinations)
        {
            Sources = sources;
            Destinations = destinations;
        }

        public List<SourceNode> Sources { get; }
        public List<DestinationNode> Destinations { get; }
    }
}
