using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Insights;

/// <summary>
/// Rule-based inventory insights with brand/warehouse-aware recommendations and optional snapshot cache.
/// </summary>
public class InventoryInsightsAppService : IInventoryInsightsAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IInventoryInsightReadRepository _inventoryInsightReadRepository;
    private readonly IBrandScopeContext _brandScopeContext;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ITransferPolicyRepository _transferPolicyRepository;
    private readonly IInsightSnapshotRepository _insightSnapshotRepository;
    private readonly IInsightRecommendationEngine _recommendationEngine;
    private readonly InsightSnapshotOptions _snapshotOptions;
    private readonly ILogger<InventoryInsightsAppService> _logger;

    public InventoryInsightsAppService(
        IInventoryInsightReadRepository inventoryInsightReadRepository,
        IBrandScopeContext brandScopeContext,
        IWarehouseRepository warehouseRepository,
        ITransferPolicyRepository transferPolicyRepository,
        IInsightSnapshotRepository insightSnapshotRepository,
        IInsightRecommendationEngine recommendationEngine,
        IOptions<InsightSnapshotOptions> snapshotOptions,
        ILogger<InventoryInsightsAppService> logger)
    {
        _inventoryInsightReadRepository = inventoryInsightReadRepository;
        _brandScopeContext = brandScopeContext;
        _warehouseRepository = warehouseRepository;
        _transferPolicyRepository = transferPolicyRepository;
        _insightSnapshotRepository = insightSnapshotRepository;
        _recommendationEngine = recommendationEngine;
        _snapshotOptions = snapshotOptions.Value;
        _logger = logger;
    }

    public async Task<List<DeadStockInsightDto>> GetDeadStockAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int daysWithoutOutbound = 60,
        decimal minOnHand = 1,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var snapshotKey = InsightSnapshotKeyBuilder.BuildDeadStockKey(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            daysWithoutOutbound,
            minOnHand,
            maxResults);

        if (!forceRefresh)
        {
            var cached = await TryReadSnapshotAsync<List<DeadStockInsightDto>>(
                snapshotKey,
                InsightSnapshotKeyBuilder.KindDeadStock,
                cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var context = await BuildRecommendationContextAsync(
            null,
            null,
            scopedBrandId,
            scopedRegionCode,
            30,
            14,
            7,
            cancellationToken);

        var normalizedDays = NormalizePositive(daysWithoutOutbound, 60);
        var normalizedMinOnHand = minOnHand <= 0 ? 1 : minOnHand;
        var normalizedMaxResults = NormalizePositive(maxResults, 50, 200);
        var referenceDateUtc = DateTime.UtcNow;

        var facts = await _inventoryInsightReadRepository.GetDeadStockFactsAsync(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            referenceDateUtc,
            normalizedDays,
            normalizedMinOnHand,
            normalizedMaxResults,
            cancellationToken);

        var result = facts
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
                    BrandId = x.BrandId,
                    QuantityOnHand = x.QuantityOnHand,
                    QuantityAvailable = x.QuantityAvailable,
                    LastOutboundAt = x.LastOutboundAt,
                    DaysWithoutOutbound = days,
                    CostPrice = x.CostPrice,
                    EstimatedCostValue = x.CostPrice.HasValue ? x.CostPrice.Value * x.QuantityOnHand : null,
                    Severity = days >= 120 ? "critical" : "warning",
                    RuleCode = "dead_stock"
                };

                ApplyRecommendation(insight, context);
                return insight;
            })
            .OrderByDescending(x => x.Recommendation.Priority)
            .ThenByDescending(x => x.DaysWithoutOutbound)
            .ThenByDescending(x => x.EstimatedCostValue ?? 0)
            .ToList();

        await SaveSnapshotAsync(snapshotKey, InsightSnapshotKeyBuilder.KindDeadStock, result, cancellationToken);
        return result;
    }

    public async Task<List<SalesVelocityInsightDto>> GetSalesVelocityAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 100,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var snapshotKey = InsightSnapshotKeyBuilder.BuildSalesVelocityKey(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            lookbackDays,
            maxResults);

        if (!forceRefresh)
        {
            var cached = await TryReadSnapshotAsync<List<SalesVelocityInsightDto>>(
                snapshotKey,
                InsightSnapshotKeyBuilder.KindSalesVelocity,
                cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var normalizedLookbackDays = NormalizePositive(lookbackDays, 30);
        var normalizedMaxResults = NormalizePositive(maxResults, 100, 300);
        var context = await BuildRecommendationContextAsync(
            null,
            null,
            scopedBrandId,
            scopedRegionCode,
            normalizedLookbackDays,
            14,
            7,
            cancellationToken);

        var toDateUtc = DateTime.UtcNow;
        var fromDateUtc = toDateUtc.Date.AddDays(-normalizedLookbackDays);

        var facts = await _inventoryInsightReadRepository.GetSalesVelocityFactsAsync(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            fromDateUtc,
            toDateUtc,
            normalizedMaxResults,
            cancellationToken);

        var result = facts
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
                    BrandId = x.BrandId,
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

                ApplyRecommendation(insight, context);
                return insight;
            })
            .Where(x => x.OutboundQuantity > 0 || x.QuantityOnHand > 0)
            .OrderByDescending(x => x.Recommendation.Priority)
            .ThenByDescending(x => x.OutboundQuantity)
            .ThenBy(x => x.EstimatedDaysOfCover ?? decimal.MaxValue)
            .Take(normalizedMaxResults)
            .ToList();

        await SaveSnapshotAsync(snapshotKey, InsightSnapshotKeyBuilder.KindSalesVelocity, result, cancellationToken);
        return result;
    }

    public async Task<List<TransferSuggestionDto>> GetTransferSuggestionsAsync(
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int targetCoverDays = 14,
        int reserveCoverDays = 7,
        int maxResults = 20,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var snapshotKey = InsightSnapshotKeyBuilder.BuildTransferKey(
            sourceWarehouseId,
            destinationWarehouseId,
            scopedBrandId,
            scopedRegionCode,
            lookbackDays,
            targetCoverDays,
            reserveCoverDays,
            maxResults);

        if (!forceRefresh)
        {
            var cached = await TryReadSnapshotAsync<List<TransferSuggestionDto>>(
                snapshotKey,
                InsightSnapshotKeyBuilder.KindTransfer,
                cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var context = await BuildRecommendationContextAsync(
            sourceWarehouseId,
            destinationWarehouseId,
            scopedBrandId,
            scopedRegionCode,
            lookbackDays,
            targetCoverDays,
            reserveCoverDays,
            cancellationToken);

        var result = await ComputeTransferSuggestionsAsync(
            sourceWarehouseId,
            destinationWarehouseId,
            scopedBrandId,
            scopedRegionCode,
            lookbackDays,
            targetCoverDays,
            reserveCoverDays,
            maxResults,
            context,
            cancellationToken);

        await SaveSnapshotAsync(snapshotKey, InsightSnapshotKeyBuilder.KindTransfer, result, cancellationToken);
        return result;
    }

    private async Task<List<TransferSuggestionDto>> ComputeTransferSuggestionsAsync(
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        Guid? brandId,
        string? regionCode,
        int lookbackDays,
        int targetCoverDays,
        int reserveCoverDays,
        int maxResults,
        InsightRecommendationContext context,
        CancellationToken cancellationToken)
    {
        var normalizedLookbackDays = NormalizePositive(lookbackDays, 30);
        var normalizedTargetCoverDays = NormalizePositive(targetCoverDays, 14, 90);
        var normalizedReserveCoverDays = NormalizePositive(reserveCoverDays, 7, 60);
        var normalizedMaxResults = NormalizePositive(maxResults, 20, 100);

        var toDateUtc = DateTime.UtcNow;
        var fromDateUtc = toDateUtc.Date.AddDays(-normalizedLookbackDays);

        var facts = await _inventoryInsightReadRepository.GetSalesVelocityFactsAsync(
            null,
            brandId,
            regionCode,
            fromDateUtc,
            toDateUtc,
            5000,
            cancellationToken);

        var groupedFacts = facts.GroupBy(x => x.ProductVariantId).ToList();
        var suggestions = new List<TransferSuggestionDto>();

        foreach (var skuGroup in groupedFacts)
        {
            var productBrandId = skuGroup.First().BrandId;

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
                    var coverDays = averageDailyOutbound > 0
                        ? x.QuantityAvailable / averageDailyOutbound
                        : (decimal?)null;

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
                var source = sources.FirstOrDefault(x =>
                    x.Fact.WarehouseId != destination.Fact.WarehouseId
                    && x.RemainingQuantity > 0
                    && InsightTransferRules.CanTransferBetweenWarehouses(
                        x.Fact.BrandId,
                        destination.Fact.BrandId,
                        productBrandId,
                        x.Fact.RegionCode,
                        destination.Fact.RegionCode,
                        context.TransferPolicies));
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

                ApplyRecommendation(suggestion, context);
                suggestions.Add(suggestion);
            }
        }

        return suggestions
            .OrderByDescending(x => x.Recommendation.Priority)
            .ThenByDescending(x => x.SuggestedQuantity)
            .ThenBy(x => x.DestinationDaysOfCover ?? decimal.MaxValue)
            .Take(normalizedMaxResults)
            .ToList();
    }

    private async Task<InsightRecommendationContext> BuildRecommendationContextAsync(
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        Guid? brandId,
        string? regionCode,
        int lookbackDays,
        int targetCoverDays,
        int reserveCoverDays,
        CancellationToken cancellationToken)
    {
        var warehouses = await _warehouseRepository.GetListAsync(cancellationToken);
        var policies = await _transferPolicyRepository.GetActivePoliciesAsync(cancellationToken);
        var bootstrapContext = new InsightRecommendationContext
        {
            Warehouses = warehouses,
            TransferPolicies = policies,
            ReplenishTransferByDestinationKey = new Dictionary<string, TransferSuggestionDto>()
        };

        var transfers = await ComputeTransferSuggestionsAsync(
            sourceWarehouseId,
            destinationWarehouseId,
            brandId,
            regionCode,
            lookbackDays,
            targetCoverDays,
            reserveCoverDays,
            200,
            bootstrapContext,
            cancellationToken);

        var replenishMap = transfers.ToDictionary(
            x => InsightRecommendationContext.BuildReplenishKey(x.ProductVariantId, x.DestinationWarehouseId),
            x => x);

        return new InsightRecommendationContext
        {
            Warehouses = warehouses,
            TransferPolicies = policies,
            ReplenishTransferByDestinationKey = replenishMap
        };
    }

    private void ApplyRecommendation(DeadStockInsightDto insight, InsightRecommendationContext context)
    {
        var recommendation = _recommendationEngine.BuildDeadStock(insight, context);
        insight.Recommendation = recommendation;
        insight.RecommendedActionCode = recommendation.ActionCode;
        insight.RecommendationParams = recommendation.Params;
    }

    private void ApplyRecommendation(SalesVelocityInsightDto insight, InsightRecommendationContext context)
    {
        var recommendation = _recommendationEngine.BuildSalesVelocity(insight, context);
        insight.Recommendation = recommendation;
        insight.RecommendedActionCode = recommendation.ActionCode;
        insight.RecommendationParams = recommendation.Params;
    }

    private void ApplyRecommendation(TransferSuggestionDto insight, InsightRecommendationContext context)
    {
        var recommendation = _recommendationEngine.BuildTransfer(insight, context);
        insight.Recommendation = recommendation;
        insight.RecommendedActionCode = recommendation.ActionCode;
        insight.RecommendationParams = recommendation.Params;
    }

    private async Task<T?> TryReadSnapshotAsync<T>(
        string snapshotKey,
        string insightKind,
        CancellationToken cancellationToken)
    {
        if (!_snapshotOptions.Enabled || !_snapshotOptions.UseSnapshotOnRead)
        {
            return default;
        }

        var snapshot = await _insightSnapshotRepository.GetByKeyAsync(snapshotKey, cancellationToken);
        if (snapshot is null)
        {
            return default;
        }

        var age = DateTime.UtcNow - snapshot.GeneratedAtUtc;
        if (age > TimeSpan.FromMinutes(Math.Max(5, _snapshotOptions.MaxSnapshotAgeMinutes)))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(snapshot.PayloadJson, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize insight snapshot {SnapshotKey}.", snapshotKey);
            return default;
        }
    }

    private async Task SaveSnapshotAsync<T>(
        string snapshotKey,
        string insightKind,
        T payload,
        CancellationToken cancellationToken)
    {
        if (!_snapshotOptions.Enabled)
        {
            return;
        }

        try
        {
            await _insightSnapshotRepository.UpsertAsync(new Domain.Entities.InsightSnapshot
            {
                SnapshotKey = snapshotKey,
                InsightKind = insightKind,
                PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
                GeneratedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save insight snapshot {SnapshotKey}.", snapshotKey);
        }
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
