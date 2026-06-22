using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Insights;

/// <summary>
/// Rule-based inventory insights that remain deterministic and reusable by future AI orchestration.
/// </summary>
public class InventoryInsightsAppService : IInventoryInsightsAppService
{
    private readonly IInventoryInsightReadRepository _inventoryInsightReadRepository;

    public InventoryInsightsAppService(IInventoryInsightReadRepository inventoryInsightReadRepository)
    {
        _inventoryInsightReadRepository = inventoryInsightReadRepository;
    }

    public async Task<List<DeadStockInsightDto>> GetDeadStockAsync(
        Guid? warehouseId = null,
        int daysWithoutOutbound = 60,
        decimal minOnHand = 1,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedDays = NormalizePositive(daysWithoutOutbound, 60);
        var normalizedMinOnHand = minOnHand <= 0 ? 1 : minOnHand;
        var normalizedMaxResults = NormalizePositive(maxResults, 50, 200);
        var referenceDateUtc = DateTime.UtcNow;

        var facts = await _inventoryInsightReadRepository.GetDeadStockFactsAsync(
            warehouseId,
            referenceDateUtc,
            normalizedDays,
            normalizedMinOnHand,
            normalizedMaxResults,
            cancellationToken);

        return facts
            .Select(x =>
            {
                var days = x.LastOutboundAt.HasValue
                    ? Math.Max(normalizedDays, (referenceDateUtc.Date - x.LastOutboundAt.Value.Date).Days)
                    : normalizedDays;

                var insight = new DeadStockInsightDto
                {
                    ProductVariantId = x.ProductVariantId,
                    Sku = x.Sku,
                    WarehouseId = x.WarehouseId,
                    WarehouseCode = x.WarehouseCode,
                    WarehouseName = x.WarehouseName,
                    QuantityOnHand = x.QuantityOnHand,
                    QuantityAvailable = x.QuantityAvailable,
                    LastOutboundAt = x.LastOutboundAt,
                    DaysWithoutOutbound = days,
                    CostPrice = x.CostPrice,
                    EstimatedCostValue = x.CostPrice.HasValue ? x.CostPrice.Value * x.QuantityOnHand : null,
                    Severity = days >= 120 ? "critical" : "warning",
                    RuleCode = "dead_stock"
                };

                ApplyRecommendation(insight);
                return insight;
            })
            .OrderByDescending(x => x.DaysWithoutOutbound)
            .ThenByDescending(x => x.EstimatedCostValue ?? 0)
            .ToList();
    }

    public async Task<List<SalesVelocityInsightDto>> GetSalesVelocityAsync(
        Guid? warehouseId = null,
        int lookbackDays = 30,
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        var normalizedLookbackDays = NormalizePositive(lookbackDays, 30);
        var normalizedMaxResults = NormalizePositive(maxResults, 100, 300);
        var toDateUtc = DateTime.UtcNow;
        var fromDateUtc = toDateUtc.Date.AddDays(-normalizedLookbackDays);

        var facts = await _inventoryInsightReadRepository.GetSalesVelocityFactsAsync(
            warehouseId,
            fromDateUtc,
            toDateUtc,
            normalizedMaxResults,
            cancellationToken);

        return facts
            .Select(x =>
            {
                var averageDailyOutbound = x.OutboundQuantity / normalizedLookbackDays;
                var estimatedDaysOfCover = averageDailyOutbound > 0
                    ? x.QuantityAvailable / averageDailyOutbound
                    : (decimal?)null;

                var insight = new SalesVelocityInsightDto
                {
                    ProductVariantId = x.ProductVariantId,
                    Sku = x.Sku,
                    WarehouseId = x.WarehouseId,
                    WarehouseCode = x.WarehouseCode,
                    WarehouseName = x.WarehouseName,
                    QuantityOnHand = x.QuantityOnHand,
                    QuantityAvailable = x.QuantityAvailable,
                    OutboundQuantity = x.OutboundQuantity,
                    AverageDailyOutbound = averageDailyOutbound,
                    EstimatedDaysOfCover = estimatedDaysOfCover,
                    LastOutboundAt = x.LastOutboundAt,
                    LookbackDays = normalizedLookbackDays,
                    Severity = GetVelocitySeverity(averageDailyOutbound, estimatedDaysOfCover),
                    RuleCode = "sales_velocity"
                };

                ApplyRecommendation(insight);
                return insight;
            })
            .Where(x => x.OutboundQuantity > 0 || x.QuantityOnHand > 0)
            .OrderByDescending(x => x.OutboundQuantity)
            .ThenBy(x => x.EstimatedDaysOfCover ?? decimal.MaxValue)
            .Take(normalizedMaxResults)
            .ToList();
    }

    public async Task<List<TransferSuggestionDto>> GetTransferSuggestionsAsync(
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        int lookbackDays = 30,
        int targetCoverDays = 14,
        int reserveCoverDays = 7,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedLookbackDays = NormalizePositive(lookbackDays, 30);
        var normalizedTargetCoverDays = NormalizePositive(targetCoverDays, 14, 90);
        var normalizedReserveCoverDays = NormalizePositive(reserveCoverDays, 7, 60);
        var normalizedMaxResults = NormalizePositive(maxResults, 20, 100);

        var toDateUtc = DateTime.UtcNow;
        var fromDateUtc = toDateUtc.Date.AddDays(-normalizedLookbackDays);

        var facts = await _inventoryInsightReadRepository.GetSalesVelocityFactsAsync(
            null,
            fromDateUtc,
            toDateUtc,
            5000,
            cancellationToken);

        var groupedFacts = facts
            .GroupBy(x => x.ProductVariantId)
            .ToList();

        var suggestions = new List<TransferSuggestionDto>();

        foreach (var skuGroup in groupedFacts)
        {
            var sources = skuGroup
                .Where(x => !sourceWarehouseId.HasValue || x.WarehouseId == sourceWarehouseId.Value)
                .Select(x => new TransferCandidate(
                    x,
                    Math.Max(
                        0,
                        x.QuantityAvailable - (x.OutboundQuantity > 0
                            ? (x.OutboundQuantity / normalizedLookbackDays) * normalizedReserveCoverDays
                            : 0))))
                .Where(x => x.RemainingQuantity > 0)
                .OrderByDescending(x => x.RemainingQuantity)
                .ToList();

            var destinations = skuGroup
                .Where(x => !destinationWarehouseId.HasValue || x.WarehouseId == destinationWarehouseId.Value)
                .Select(x =>
                {
                    var averageDailyOutbound = x.OutboundQuantity / normalizedLookbackDays;
                    var desiredStock = averageDailyOutbound * normalizedTargetCoverDays;
                    var neededQuantity = Math.Max(0, desiredStock - x.QuantityAvailable);
                    var coverDays = averageDailyOutbound > 0 ? x.QuantityAvailable / averageDailyOutbound : (decimal?)null;

                    return new
                    {
                        Fact = x,
                        AverageDailyOutbound = averageDailyOutbound,
                        NeededQuantity = neededQuantity,
                        CoverDays = coverDays
                    };
                })
                .Where(x => x.AverageDailyOutbound > 0 && x.NeededQuantity > 0)
                .OrderBy(x => x.CoverDays ?? decimal.MaxValue)
                .ThenByDescending(x => x.NeededQuantity)
                .ToList();

            foreach (var destination in destinations)
            {
                var source = sources.FirstOrDefault(x => x.Fact.WarehouseId != destination.Fact.WarehouseId && x.RemainingQuantity > 0);
                if (source is null)
                {
                    continue;
                }

                var suggestedQuantity = Math.Min(source.RemainingQuantity, destination.NeededQuantity);
                if (suggestedQuantity <= 0)
                {
                    continue;
                }

                source.RemainingQuantity -= suggestedQuantity;

                var suggestion = new TransferSuggestionDto
                {
                    ProductVariantId = destination.Fact.ProductVariantId,
                    Sku = destination.Fact.Sku,
                    SourceWarehouseId = source.Fact.WarehouseId,
                    SourceWarehouseCode = source.Fact.WarehouseCode,
                    SourceWarehouseName = source.Fact.WarehouseName,
                    DestinationWarehouseId = destination.Fact.WarehouseId,
                    DestinationWarehouseCode = destination.Fact.WarehouseCode,
                    DestinationWarehouseName = destination.Fact.WarehouseName,
                    SuggestedQuantity = suggestedQuantity,
                    SourceAvailable = source.Fact.QuantityAvailable,
                    DestinationAvailable = destination.Fact.QuantityAvailable,
                    DestinationAverageDailyOutbound = destination.AverageDailyOutbound,
                    DestinationDaysOfCover = destination.CoverDays,
                    Severity = destination.CoverDays.HasValue && destination.CoverDays.Value < 7 ? "critical" : "warning",
                    RuleCode = "transfer_rebalance"
                };

                ApplyRecommendation(suggestion);
                suggestions.Add(suggestion);
            }
        }

        return suggestions
            .OrderByDescending(x => x.SuggestedQuantity)
            .ThenBy(x => x.DestinationDaysOfCover ?? decimal.MaxValue)
            .Take(normalizedMaxResults)
            .ToList();
    }

    private static int NormalizePositive(int value, int fallback, int? maxValue = null)
    {
        var normalized = value <= 0 ? fallback : value;
        return maxValue.HasValue ? Math.Min(normalized, maxValue.Value) : normalized;
    }

    private static string GetVelocitySeverity(decimal averageDailyOutbound, decimal? estimatedDaysOfCover)
    {
        if (averageDailyOutbound <= 0)
        {
            return "info";
        }

        if (estimatedDaysOfCover.HasValue && estimatedDaysOfCover.Value < 7)
        {
            return "critical";
        }

        if (estimatedDaysOfCover.HasValue && estimatedDaysOfCover.Value < 14)
        {
            return "warning";
        }

        return "info";
    }

    private static void ApplyRecommendation(DeadStockInsightDto insight)
    {
        var (actionCode, parameters) = InsightRecommendationBuilder.ForDeadStock(insight);
        insight.RecommendedActionCode = actionCode;
        insight.RecommendationParams = parameters;
    }

    private static void ApplyRecommendation(SalesVelocityInsightDto insight)
    {
        var (actionCode, parameters) = InsightRecommendationBuilder.ForSalesVelocity(insight);
        insight.RecommendedActionCode = actionCode;
        insight.RecommendationParams = parameters;
    }

    private static void ApplyRecommendation(TransferSuggestionDto insight)
    {
        var (actionCode, parameters) = InsightRecommendationBuilder.ForTransfer(insight);
        insight.RecommendedActionCode = actionCode;
        insight.RecommendationParams = parameters;
    }

    private sealed class TransferCandidate
    {
        public TransferCandidate(SalesVelocityFact fact, decimal remainingQuantity)
        {
            Fact = fact;
            RemainingQuantity = remainingQuantity;
        }

        public SalesVelocityFact Fact { get; }

        public decimal RemainingQuantity { get; set; }
    }
}
