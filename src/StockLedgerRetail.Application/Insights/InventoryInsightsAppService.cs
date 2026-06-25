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
                    CurrentSellingPriceBeforeVat = x.CurrentSellingPriceBeforeVat,
                    CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                    VatRate = x.VatRate,
                    EstimatedRevenueValue = x.CurrentSellingPriceAfterVat.HasValue ? x.CurrentSellingPriceAfterVat.Value * x.QuantityOnHand : null,
                    EstimatedMarginValue = x.CurrentSellingPriceBeforeVat.HasValue && x.CostPrice.HasValue
                        ? (x.CurrentSellingPriceBeforeVat.Value - x.CostPrice.Value) * x.QuantityOnHand
                        : null,
                    MarginRate = x.CurrentSellingPriceBeforeVat.HasValue && x.CostPrice.HasValue && x.CurrentSellingPriceBeforeVat.Value > 0
                        ? ((x.CurrentSellingPriceBeforeVat.Value - x.CostPrice.Value) / x.CurrentSellingPriceBeforeVat.Value) * 100
                        : null,
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
                    CostPrice = x.CostPrice,
                    CurrentSellingPriceBeforeVat = x.CurrentSellingPriceBeforeVat,
                    CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                    VatRate = x.VatRate,
                    RevenueOpportunity = x.CurrentSellingPriceAfterVat.HasValue ? x.CurrentSellingPriceAfterVat.Value * x.OutboundQuantity : null,
                    MarginPerUnit = x.CurrentSellingPriceBeforeVat.HasValue && x.CostPrice.HasValue
                        ? x.CurrentSellingPriceBeforeVat.Value - x.CostPrice.Value
                        : null,
                    InventoryValue = x.InventoryValue,
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

    public async Task<List<MarkdownCandidateInsightDto>> GetMarkdownCandidatesAsync(
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
        var snapshotKey = InsightSnapshotKeyBuilder.BuildMarkdownCandidatesKey(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            daysWithoutOutbound,
            minOnHand,
            maxResults);

        if (!forceRefresh)
        {
            var cached = await TryReadSnapshotAsync<List<MarkdownCandidateInsightDto>>(
                snapshotKey,
                InsightSnapshotKeyBuilder.KindMarkdownCandidates,
                cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var normalizedDays = NormalizePositive(daysWithoutOutbound, 60);
        var normalizedMinOnHand = minOnHand <= 0 ? 1 : minOnHand;
        var normalizedMaxResults = NormalizePositive(maxResults, 50, 200);
        var referenceDateUtc = DateTime.UtcNow;
        var context = await BuildRecommendationContextAsync(
            null,
            null,
            scopedBrandId,
            scopedRegionCode,
            30,
            14,
            7,
            cancellationToken);

        var facts = await _inventoryInsightReadRepository.GetMarkdownCandidateFactsAsync(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            referenceDateUtc,
            normalizedDays,
            normalizedMinOnHand,
            normalizedMaxResults,
            cancellationToken);

        var result = facts.Select(x =>
        {
            var days = x.LastOutboundAt.HasValue
                ? Math.Max(normalizedDays, (referenceDateUtc.Date - x.LastOutboundAt.Value.Date).Days)
                : normalizedDays;
            var markdownDepth = days >= 120 ? 25m : 15m;
            var markdownBeforeVat = x.CurrentSellingPriceBeforeVat.HasValue
                ? RoundMoney(x.CurrentSellingPriceBeforeVat.Value * (1 - markdownDepth / 100m))
                : (decimal?)null;
            var markdownAfterVat = markdownBeforeVat.HasValue && x.VatRate.HasValue
                ? RoundMoney(markdownBeforeVat.Value * (1 + x.VatRate.Value / 100m))
                : markdownBeforeVat;

            var insight = new MarkdownCandidateInsightDto
            {
                ProductVariantId = x.ProductVariantId,
                Sku = x.Sku,
                WarehouseId = x.WarehouseId,
                WarehouseCode = x.WarehouseCode,
                WarehouseName = x.WarehouseName,
                BrandId = x.BrandId,
                QuantityOnHand = x.QuantityOnHand,
                DaysWithoutOutbound = days,
                CostPrice = x.CostPrice,
                CurrentSellingPriceBeforeVat = x.CurrentSellingPriceBeforeVat,
                CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                VatRate = x.VatRate,
                SuggestedMarkdownPriceBeforeVat = markdownBeforeVat,
                SuggestedMarkdownPriceAfterVat = markdownAfterVat,
                MarkdownDepthPercent = markdownDepth,
                EstimatedInventoryValue = x.InventoryValue,
                EstimatedRecoveryValue = markdownAfterVat.HasValue ? markdownAfterVat.Value * x.QuantityOnHand : null,
                Severity = days >= 120 ? "critical" : "warning",
                RuleCode = "markdown_candidate"
            };

            ApplyRecommendation(insight, context);
            return insight;
        })
        .OrderByDescending(x => x.Recommendation.Priority)
        .ThenByDescending(x => x.EstimatedInventoryValue ?? 0)
        .ToList();

        await SaveSnapshotAsync(snapshotKey, InsightSnapshotKeyBuilder.KindMarkdownCandidates, result, cancellationToken);
        return result;
    }

    public async Task<List<PromotionRiskInsightDto>> GetPromotionRiskAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var snapshotKey = InsightSnapshotKeyBuilder.BuildPromotionRiskKey(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            lookbackDays,
            maxResults);

        if (!forceRefresh)
        {
            var cached = await TryReadSnapshotAsync<List<PromotionRiskInsightDto>>(
                snapshotKey,
                InsightSnapshotKeyBuilder.KindPromotionRisk,
                cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var normalizedLookbackDays = NormalizePositive(lookbackDays, 30);
        var normalizedMaxResults = NormalizePositive(maxResults, 50, 200);
        var toDateUtc = DateTime.UtcNow;
        var fromDateUtc = toDateUtc.Date.AddDays(-normalizedLookbackDays);
        var context = await BuildRecommendationContextAsync(
            null,
            null,
            scopedBrandId,
            scopedRegionCode,
            normalizedLookbackDays,
            14,
            7,
            cancellationToken);

        var facts = await _inventoryInsightReadRepository.GetPromotionRiskFactsAsync(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            fromDateUtc,
            toDateUtc,
            normalizedMaxResults,
            cancellationToken);

        var result = facts.Select(x =>
        {
            var averageDailyOutbound = x.OutboundQuantity / normalizedLookbackDays;
            var coverDays = averageDailyOutbound > 0 ? x.QuantityAvailable / averageDailyOutbound : (decimal?)null;
            var discountPercent = x.RegularPriceAfterVat.HasValue && x.PromotionPriceAfterVat.HasValue && x.RegularPriceAfterVat.Value > 0
                ? ((x.RegularPriceAfterVat.Value - x.PromotionPriceAfterVat.Value) / x.RegularPriceAfterVat.Value) * 100
                : (decimal?)null;
            var marginRate = x.PromotionPriceBeforeVat.HasValue && x.CostPrice.HasValue && x.PromotionPriceBeforeVat.Value > 0
                ? ((x.PromotionPriceBeforeVat.Value - x.CostPrice.Value) / x.PromotionPriceBeforeVat.Value) * 100
                : (decimal?)null;

            var insight = new PromotionRiskInsightDto
            {
                ProductVariantId = x.ProductVariantId,
                Sku = x.Sku,
                WarehouseId = x.WarehouseId,
                WarehouseCode = x.WarehouseCode,
                WarehouseName = x.WarehouseName,
                BrandId = x.BrandId,
                QuantityAvailable = x.QuantityAvailable,
                OutboundQuantity = x.OutboundQuantity,
                AverageDailyOutbound = averageDailyOutbound,
                EstimatedDaysOfCover = coverDays,
                CostPrice = x.CostPrice,
                RegularPriceBeforeVat = x.RegularPriceBeforeVat,
                RegularPriceAfterVat = x.RegularPriceAfterVat,
                PromotionPriceBeforeVat = x.PromotionPriceBeforeVat,
                PromotionPriceAfterVat = x.PromotionPriceAfterVat,
                VatRate = x.VatRate,
                PromotionDiscountPercent = discountPercent,
                MarginRateAfterPromotion = marginRate,
                Severity = coverDays.HasValue && coverDays.Value < 7 ? "critical" : "warning",
                RuleCode = "promotion_risk"
            };

            ApplyRecommendation(insight, context);
            return insight;
        })
        .OrderByDescending(x => x.Recommendation.Priority)
        .ThenBy(x => x.EstimatedDaysOfCover ?? decimal.MaxValue)
        .ToList();

        await SaveSnapshotAsync(snapshotKey, InsightSnapshotKeyBuilder.KindPromotionRisk, result, cancellationToken);
        return result;
    }

    public async Task<List<ReorderRiskInsightDto>> GetReorderRiskAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var snapshotKey = InsightSnapshotKeyBuilder.BuildReorderRiskKey(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            lookbackDays,
            maxResults);

        if (!forceRefresh)
        {
            var cached = await TryReadSnapshotAsync<List<ReorderRiskInsightDto>>(
                snapshotKey,
                InsightSnapshotKeyBuilder.KindReorderRisk,
                cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var normalizedLookbackDays = NormalizePositive(lookbackDays, 30);
        var normalizedMaxResults = NormalizePositive(maxResults, 50, 200);
        var toDateUtc = DateTime.UtcNow;
        var fromDateUtc = toDateUtc.Date.AddDays(-normalizedLookbackDays);
        var context = await BuildRecommendationContextAsync(
            null,
            null,
            scopedBrandId,
            scopedRegionCode,
            normalizedLookbackDays,
            14,
            7,
            cancellationToken);

        var facts = await _inventoryInsightReadRepository.GetReorderRiskFactsAsync(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            fromDateUtc,
            toDateUtc,
            normalizedMaxResults,
            cancellationToken);

        var result = facts.Select(x =>
        {
            var averageDailyOutbound = x.OutboundQuantity / normalizedLookbackDays;
            var coverDays = averageDailyOutbound > 0 ? x.QuantityAvailable / averageDailyOutbound : (decimal?)null;
            var reorderPoint = averageDailyOutbound * 14;
            var suggestedReorderQty = Math.Max(0, reorderPoint + averageDailyOutbound * 7 - x.QuantityAvailable - x.QuantityOnOrder - x.QuantityInReceiving);

            var insight = new ReorderRiskInsightDto
            {
                ProductVariantId = x.ProductVariantId,
                Sku = x.Sku,
                WarehouseId = x.WarehouseId,
                WarehouseCode = x.WarehouseCode,
                WarehouseName = x.WarehouseName,
                BrandId = x.BrandId,
                QuantityAvailable = x.QuantityAvailable,
                QuantityOnOrder = x.QuantityOnOrder,
                QuantityInReceiving = x.QuantityInReceiving,
                AverageDailyOutbound = averageDailyOutbound,
                EstimatedDaysOfCover = coverDays,
                ReorderPoint = reorderPoint,
                SuggestedReorderQuantity = suggestedReorderQty,
                CostPrice = x.CostPrice,
                CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                Severity = coverDays.HasValue && coverDays.Value < 7 ? "critical" : coverDays.HasValue && coverDays.Value < 14 ? "warning" : "info",
                RuleCode = "reorder_risk"
            };

            ApplyRecommendation(insight, context);
            return insight;
        })
        .Where(x => x.AverageDailyOutbound > 0)
        .OrderByDescending(x => x.Recommendation.Priority)
        .ThenBy(x => x.EstimatedDaysOfCover ?? decimal.MaxValue)
        .ToList();

        await SaveSnapshotAsync(snapshotKey, InsightSnapshotKeyBuilder.KindReorderRisk, result, cancellationToken);
        return result;
    }

    public async Task<List<TrendSummaryInsightDto>> GetTrendSummaryAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var snapshotKey = InsightSnapshotKeyBuilder.BuildTrendSummaryKey(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            lookbackDays,
            maxResults);

        if (!forceRefresh)
        {
            var cached = await TryReadSnapshotAsync<List<TrendSummaryInsightDto>>(
                snapshotKey,
                InsightSnapshotKeyBuilder.KindTrendSummary,
                cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var normalizedLookbackDays = NormalizePositive(lookbackDays, 30);
        var normalizedMaxResults = NormalizePositive(maxResults, 50, 200);
        var currentToDateUtc = DateTime.UtcNow;
        var currentFromDateUtc = currentToDateUtc.Date.AddDays(-normalizedLookbackDays);
        var previousToDateUtc = currentFromDateUtc.AddDays(-1);
        var previousFromDateUtc = previousToDateUtc.Date.AddDays(-normalizedLookbackDays);
        var context = await BuildRecommendationContextAsync(
            null,
            null,
            scopedBrandId,
            scopedRegionCode,
            normalizedLookbackDays,
            14,
            7,
            cancellationToken);

        var facts = await _inventoryInsightReadRepository.GetTrendSummaryFactsAsync(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            currentFromDateUtc,
            currentToDateUtc,
            previousFromDateUtc,
            previousToDateUtc,
            normalizedMaxResults,
            cancellationToken);

        var result = facts.Select(x =>
        {
            var currentAverage = x.CurrentOutboundQuantity / normalizedLookbackDays;
            var previousAverage = x.PreviousOutboundQuantity / normalizedLookbackDays;
            var outboundTrend = previousAverage > 0
                ? ((currentAverage - previousAverage) / previousAverage) * 100
                : currentAverage > 0 ? 100 : 0;
            var inventoryDelta = x.CurrentInventoryValue - x.PreviousInventoryValue;
            var priceTrend = x.PreviousSellingPriceAfterVat.HasValue && x.PreviousSellingPriceAfterVat.Value > 0 && x.CurrentSellingPriceAfterVat.HasValue
                ? ((x.CurrentSellingPriceAfterVat.Value - x.PreviousSellingPriceAfterVat.Value) / x.PreviousSellingPriceAfterVat.Value) * 100
                : 0;

            var insight = new TrendSummaryInsightDto
            {
                ProductVariantId = x.ProductVariantId,
                Sku = x.Sku,
                WarehouseId = x.WarehouseId,
                WarehouseCode = x.WarehouseCode,
                WarehouseName = x.WarehouseName,
                BrandId = x.BrandId,
                CurrentQuantityOnHand = x.CurrentQuantityOnHand,
                CurrentInventoryValue = x.CurrentInventoryValue,
                PreviousInventoryValue = x.PreviousInventoryValue,
                InventoryValueDelta = inventoryDelta,
                CurrentAverageDailyOutbound = currentAverage,
                PreviousAverageDailyOutbound = previousAverage,
                OutboundTrendPercent = outboundTrend,
                CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                PreviousSellingPriceAfterVat = x.PreviousSellingPriceAfterVat,
                PriceTrendPercent = x.CurrentSellingPriceAfterVat.HasValue && x.PreviousSellingPriceAfterVat.HasValue ? priceTrend : null,
                Severity = Math.Abs(outboundTrend) >= 40 || Math.Abs(inventoryDelta) >= 1_000_000 ? "warning" : "info",
                RuleCode = "trend_summary"
            };

            ApplyRecommendation(insight, context);
            return insight;
        })
        .OrderByDescending(x => x.Recommendation.Priority)
        .ThenByDescending(x => Math.Abs(x.InventoryValueDelta))
        .ToList();

        await SaveSnapshotAsync(snapshotKey, InsightSnapshotKeyBuilder.KindTrendSummary, result, cancellationToken);
        return result;
    }

    public async Task<InsightsExecutiveSummaryDto> GetExecutiveSummaryAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int daysWithoutOutbound = 60,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var snapshotKey = InsightSnapshotKeyBuilder.BuildExecutiveSummaryKey(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            lookbackDays,
            daysWithoutOutbound);

        if (!forceRefresh)
        {
            var cached = await TryReadSnapshotAsync<InsightsExecutiveSummaryDto>(
                snapshotKey,
                InsightSnapshotKeyBuilder.KindExecutiveSummary,
                cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var deadStock = await GetDeadStockAsync(warehouseId, scopedBrandId, scopedRegionCode, daysWithoutOutbound, 1, 200, cancellationToken, forceRefresh: true);
        var promotionRisk = await GetPromotionRiskAsync(warehouseId, scopedBrandId, scopedRegionCode, lookbackDays, 200, cancellationToken, forceRefresh: true);
        var reorderRisk = await GetReorderRiskAsync(warehouseId, scopedBrandId, scopedRegionCode, lookbackDays, 200, cancellationToken, forceRefresh: true);
        var transfer = await GetTransferSuggestionsAsync(null, warehouseId, scopedBrandId, scopedRegionCode, lookbackDays, 14, 7, 200, cancellationToken, forceRefresh: true);
        var markdown = await GetMarkdownCandidatesAsync(warehouseId, scopedBrandId, scopedRegionCode, daysWithoutOutbound, 1, 200, cancellationToken, forceRefresh: true);

        var summary = new InsightsExecutiveSummaryDto
        {
            DeadStockCount = deadStock.Count,
            TiedCapital = deadStock.Sum(x => x.EstimatedCostValue ?? 0),
            InventoryValueAtRisk = deadStock.Sum(x => x.EstimatedRevenueValue ?? 0),
            MarginAtRisk = deadStock.Sum(x => x.EstimatedMarginValue ?? 0),
            PromotionRiskCount = promotionRisk.Count(x => x.Severity is "critical" or "warning"),
            ReorderRiskCount = reorderRisk.Count(x => x.Severity is "critical" or "warning"),
            TransferOpportunityCount = transfer.Count,
            TransferOpportunityValue = transfer.Sum(x => x.TransferValue ?? 0),
            MarkdownCandidateCount = markdown.Count,
            MarkdownRecoveryValue = markdown.Sum(x => x.EstimatedRecoveryValue ?? 0)
        };

        await SaveSnapshotAsync(snapshotKey, InsightSnapshotKeyBuilder.KindExecutiveSummary, summary, cancellationToken);
        return summary;
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
                    SourceCostPrice = source.Fact.CostPrice,
                    CurrentSellingPriceBeforeVat = destination.Fact.CurrentSellingPriceBeforeVat,
                    CurrentSellingPriceAfterVat = destination.Fact.CurrentSellingPriceAfterVat,
                    VatRate = destination.Fact.VatRate,
                    TransferValue = source.Fact.CostPrice.HasValue ? source.Fact.CostPrice.Value * suggestedQuantity : null,
                    MarginOpportunity = destination.Fact.CurrentSellingPriceBeforeVat.HasValue && source.Fact.CostPrice.HasValue
                        ? (destination.Fact.CurrentSellingPriceBeforeVat.Value - source.Fact.CostPrice.Value) * suggestedQuantity
                        : null,
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

    private void ApplyRecommendation(MarkdownCandidateInsightDto insight, InsightRecommendationContext context)
    {
        var recommendation = _recommendationEngine.BuildMarkdownCandidate(insight, context);
        insight.Recommendation = recommendation;
        insight.RecommendedActionCode = recommendation.ActionCode;
        insight.RecommendationParams = recommendation.Params;
    }

    private void ApplyRecommendation(PromotionRiskInsightDto insight, InsightRecommendationContext context)
    {
        var recommendation = _recommendationEngine.BuildPromotionRisk(insight, context);
        insight.Recommendation = recommendation;
        insight.RecommendedActionCode = recommendation.ActionCode;
        insight.RecommendationParams = recommendation.Params;
    }

    private void ApplyRecommendation(ReorderRiskInsightDto insight, InsightRecommendationContext context)
    {
        var recommendation = _recommendationEngine.BuildReorderRisk(insight, context);
        insight.Recommendation = recommendation;
        insight.RecommendedActionCode = recommendation.ActionCode;
        insight.RecommendationParams = recommendation.Params;
    }

    private void ApplyRecommendation(TrendSummaryInsightDto insight, InsightRecommendationContext context)
    {
        var recommendation = _recommendationEngine.BuildTrendSummary(insight, context);
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

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

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
