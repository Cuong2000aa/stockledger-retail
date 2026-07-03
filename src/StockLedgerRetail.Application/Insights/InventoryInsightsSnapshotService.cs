using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Insights;

public interface IInventoryInsightsSnapshotService
{
    Task RefreshAllScopesAsync(CancellationToken cancellationToken = default);
}

public class InventoryInsightsSnapshotService : IInventoryInsightsSnapshotService
{
    private const int LookbackDays = 30;
    private const int DaysWithoutOutbound = 60;
    private const int DeadStockMaxResults = 200;
    private const int SalesVelocityMaxResults = 100;
    private const int RiskMaxResults = 200;
    private const int TransferMaxResults = 200;
    private const int FashionMaxResults = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBrandRepository _brandRepository;
    private readonly IInsightSnapshotRepository _insightSnapshotRepository;
    private readonly IGlobalExecutiveSummaryAggregator _globalExecutiveSummaryAggregator;
    private readonly InsightSnapshotOptions _options;
    private readonly ILogger<InventoryInsightsSnapshotService> _logger;

    public InventoryInsightsSnapshotService(
        IServiceScopeFactory scopeFactory,
        IBrandRepository brandRepository,
        IInsightSnapshotRepository insightSnapshotRepository,
        IGlobalExecutiveSummaryAggregator globalExecutiveSummaryAggregator,
        IOptions<InsightSnapshotOptions> options,
        ILogger<InventoryInsightsSnapshotService> logger)
    {
        _scopeFactory = scopeFactory;
        _brandRepository = brandRepository;
        _insightSnapshotRepository = insightSnapshotRepository;
        _globalExecutiveSummaryAggregator = globalExecutiveSummaryAggregator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RefreshAllScopesAsync(CancellationToken cancellationToken = default)
    {
        var refreshedScopes = 0;
        var skippedScopes = 0;

        if (_options.RefreshGlobalScope)
        {
            if (await TryRefreshScopeAsync(null, null, cancellationToken))
            {
                refreshedScopes++;
            }
            else
            {
                skippedScopes++;
            }
        }

        var brandIds = await ResolveBrandIdsAsync(cancellationToken);
        brandIds = await FilterStaleBrandIdsAsync(brandIds, cancellationToken);

        if (_options.MaxBrandsPerRun > 0 && brandIds.Count > _options.MaxBrandsPerRun)
        {
            _logger.LogInformation(
                "Insight snapshot refresh capped at {MaxBrandsPerRun} of {CandidateBrandCount} stale brands with stock.",
                _options.MaxBrandsPerRun,
                brandIds.Count);
            brandIds = brandIds.Take(_options.MaxBrandsPerRun).ToList();
        }

        var maxConcurrency = Math.Max(1, _options.MaxConcurrentBrandScopes);
        using var gate = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var refreshTasks = brandIds.Select(async brandId =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var snapshotService = scope.ServiceProvider.GetRequiredService<BrandScopeSnapshotRefresher>();
                return await snapshotService.TryRefreshAsync(brandId, null, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        });

        var results = await Task.WhenAll(refreshTasks);
        refreshedScopes += results.Count(x => x);
        skippedScopes += results.Count(x => !x);

        if (!_options.RefreshGlobalScope)
        {
            await _globalExecutiveSummaryAggregator.RefreshAsync(
                LookbackDays,
                DaysWithoutOutbound,
                cancellationToken);
        }

        _logger.LogInformation(
            "Insight snapshot refresh finished. Refreshed {RefreshedScopes} scopes, skipped {SkippedScopes} fresh scopes, processed {BrandCount} brand candidates, globalScope={RefreshGlobalScope}.",
            refreshedScopes,
            skippedScopes,
            brandIds.Count,
            _options.RefreshGlobalScope);
    }

    private async Task<List<Guid>> ResolveBrandIdsAsync(CancellationToken cancellationToken)
    {
        if (_options.RefreshOnlyBrandsWithStock)
        {
            return await _brandRepository.GetActiveBrandIdsWithStockAsync(cancellationToken);
        }

        var brands = await _brandRepository.GetListAsync(cancellationToken);
        return brands.Select(x => x.Id).ToList();
    }

    private async Task<List<Guid>> FilterStaleBrandIdsAsync(
        IReadOnlyList<Guid> brandIds,
        CancellationToken cancellationToken)
    {
        if (!_options.SkipFreshSnapshots || brandIds.Count == 0)
        {
            return brandIds.ToList();
        }

        var keys = brandIds
            .Select(id => BuildExecutiveSummaryKey(id))
            .ToList();
        var generatedAtByKey = await _insightSnapshotRepository.GetGeneratedAtUtcByKeysAsync(keys, cancellationToken);
        var maxAge = GetMaxSnapshotAge();

        return brandIds
            .Select(id =>
            {
                var key = BuildExecutiveSummaryKey(id);
                generatedAtByKey.TryGetValue(key, out var generatedAtUtc);
                return new
                {
                    BrandId = id,
                    GeneratedAtUtc = generatedAtUtc
                };
            })
            .Where(x => x.GeneratedAtUtc == default || DateTime.UtcNow - x.GeneratedAtUtc > maxAge)
            .OrderBy(x => x.GeneratedAtUtc == default ? DateTime.MinValue : x.GeneratedAtUtc)
            .Select(x => x.BrandId)
            .ToList();
    }

    private async Task<bool> TryRefreshScopeAsync(
        Guid? brandId,
        string? regionCode,
        CancellationToken cancellationToken)
    {
        if (_options.SkipFreshSnapshots && !await IsExecutiveSnapshotStaleAsync(brandId, cancellationToken))
        {
            _logger.LogDebug(
                "Skipping fresh insight snapshot scope (brandId={BrandId}, regionCode={RegionCode}).",
                brandId,
                regionCode);
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var refresher = scope.ServiceProvider.GetRequiredService<BrandScopeSnapshotRefresher>();
        return await refresher.RefreshAsync(brandId, regionCode, cancellationToken);
    }

    private async Task<bool> IsExecutiveSnapshotStaleAsync(Guid? brandId, CancellationToken cancellationToken)
    {
        var snapshot = await _insightSnapshotRepository.GetByKeyAsync(
            BuildExecutiveSummaryKey(brandId),
            cancellationToken);

        if (snapshot is null)
        {
            return true;
        }

        return DateTime.UtcNow - snapshot.GeneratedAtUtc > GetMaxSnapshotAge();
    }

    private TimeSpan GetMaxSnapshotAge() =>
        TimeSpan.FromMinutes(Math.Max(5, _options.MaxSnapshotAgeMinutes));

    private static string BuildExecutiveSummaryKey(Guid? brandId) =>
        InsightSnapshotKeyBuilder.BuildExecutiveSummaryKey(
            null,
            brandId,
            null,
            LookbackDays,
            DaysWithoutOutbound);
}

internal class BrandScopeSnapshotRefresher
{
    private const int LookbackDays = 30;
    private const int DaysWithoutOutbound = 60;
    private const int DeadStockMaxResults = 200;
    private const int SalesVelocityMaxResults = 100;
    private const int RiskMaxResults = 200;
    private const int TransferMaxResults = 200;
    private const int FashionMaxResults = 50;

    private readonly IInventoryInsightsAppService _inventoryInsightsAppService;
    private readonly ILogger<BrandScopeSnapshotRefresher> _logger;

    public BrandScopeSnapshotRefresher(
        IInventoryInsightsAppService inventoryInsightsAppService,
        ILogger<BrandScopeSnapshotRefresher> logger)
    {
        _inventoryInsightsAppService = inventoryInsightsAppService;
        _logger = logger;
    }

    public async Task<bool> TryRefreshAsync(
        Guid brandId,
        string? regionCode,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await RefreshScopeAsync(brandId, regionCode, cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation(
            "Refreshed insight snapshots for scope brandId={BrandId}, regionCode={RegionCode} in {ElapsedMs} ms.",
            brandId,
            regionCode,
            stopwatch.ElapsedMilliseconds);
        return true;
    }

    public async Task<bool> RefreshAsync(
        Guid? brandId,
        string? regionCode,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await RefreshScopeAsync(brandId, regionCode, cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation(
            "Refreshed insight snapshots for scope brandId={BrandId}, regionCode={RegionCode} in {ElapsedMs} ms.",
            brandId,
            regionCode,
            stopwatch.ElapsedMilliseconds);
        return true;
    }

    private async Task RefreshScopeAsync(Guid? brandId, string? regionCode, CancellationToken cancellationToken)
    {
        await _inventoryInsightsAppService.GetDeadStockAsync(
            null, brandId, regionCode, DaysWithoutOutbound, 1, DeadStockMaxResults, cancellationToken, forceRefresh: true);

        await _inventoryInsightsAppService.GetSalesVelocityAsync(
            null, brandId, regionCode, LookbackDays, SalesVelocityMaxResults, cancellationToken, forceRefresh: true);

        await _inventoryInsightsAppService.GetTransferSuggestionsAsync(
            null, null, brandId, regionCode, LookbackDays, 14, 7, TransferMaxResults, cancellationToken, forceRefresh: true);

        await _inventoryInsightsAppService.GetMarkdownCandidatesAsync(
            null, brandId, regionCode, DaysWithoutOutbound, 1, DeadStockMaxResults, cancellationToken, forceRefresh: true);

        await _inventoryInsightsAppService.GetPromotionRiskAsync(
            null, brandId, regionCode, LookbackDays, RiskMaxResults, cancellationToken, forceRefresh: true);

        await _inventoryInsightsAppService.GetReorderRiskAsync(
            null, brandId, regionCode, LookbackDays, RiskMaxResults, cancellationToken, forceRefresh: true);

        await _inventoryInsightsAppService.GetTrendSummaryAsync(
            null, brandId, regionCode, LookbackDays, RiskMaxResults, cancellationToken, forceRefresh: true);

        await _inventoryInsightsAppService.GetExecutiveSummaryAsync(
            null,
            brandId,
            regionCode,
            LookbackDays,
            DaysWithoutOutbound,
            cancellationToken,
            forceRefresh: true,
            aggregateFromSnapshots: true);

        await _inventoryInsightsAppService.GetBrokenSizeRunsAsync(
            null, brandId, regionCode, LookbackDays, FashionMaxResults, cancellationToken);

        await _inventoryInsightsAppService.GetSeasonClearanceAsync(
            null, brandId, regionCode, null, LookbackDays, DaysWithoutOutbound, FashionMaxResults, cancellationToken);
    }
}
