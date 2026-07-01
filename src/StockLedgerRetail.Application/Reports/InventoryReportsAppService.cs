using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Caching;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Reports;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Reports;

public class InventoryReportsAppService : IInventoryReportsAppService
{
    private const string LogScope = "Reports";

    private readonly IInventoryReportReadRepository _inventoryReportReadRepository;
    private readonly IProductCostHistoryRepository _productCostHistoryRepository;
    private readonly IStockLotRepository _stockLotRepository;
    private readonly ILotStockRepository _lotStockRepository;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;
    private readonly IWarehouseScopeService _warehouseScopeService;
    private readonly ILogger<InventoryReportsAppService> _logger;

    public InventoryReportsAppService(
        IInventoryReportReadRepository inventoryReportReadRepository,
        IProductCostHistoryRepository productCostHistoryRepository,
        IStockLotRepository stockLotRepository,
        ILotStockRepository lotStockRepository,
        ICacheService cacheService,
        IOptions<CacheOptions> cacheOptions,
        IWarehouseScopeService warehouseScopeService,
        ILogger<InventoryReportsAppService> logger)
    {
        _inventoryReportReadRepository = inventoryReportReadRepository;
        _productCostHistoryRepository = productCostHistoryRepository;
        _stockLotRepository = stockLotRepository;
        _lotStockRepository = lotStockRepository;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
        _warehouseScopeService = warehouseScopeService;
        _logger = logger;
    }

    public async Task<InventoryValueReportDto> GetInventoryValueAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(warehouseId);
        var paging = PagingNormalizer.Normalize(page, pageSize);
        var cacheKey = CacheKeys.InventoryValue(
            scope.WarehouseId,
            brandId,
            paging.Page,
            paging.PageSize,
            scope.ScopedWarehouseIds);

        var cachedReport = await TryGetCachedReportAsync<InventoryValueReportDto>(
            reportName: "InventoryValue",
            cacheKey,
            cancellationToken);

        if (cachedReport is not null)
        {
            return cachedReport;
        }

        var reportFromDatabase = await BuildInventoryValueReportFromDatabaseAsync(
            scope.WarehouseId,
            brandId,
            scope.ScopedWarehouseIds,
            paging.Skip,
            paging.Take,
            paging.Page,
            paging.PageSize,
            cancellationToken);

        await StoreReportInCacheAsync(
            reportName: "InventoryValue",
            cacheKey,
            reportFromDatabase,
            ttl: TimeSpan.FromMinutes(_cacheOptions.ReportTtlMinutes),
            cancellationToken);

        return reportFromDatabase;
    }

    public async Task<NxtReportDto> GetNxtReportAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? warehouseId = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(warehouseId);
        var paging = PagingNormalizer.Normalize(page, pageSize);
        var dateRange = ReportDateRange.FromUserInput(fromDate, toDate);
        var cacheKey = CacheKeys.NxtReport(
            dateRange.FromInclusiveUtc,
            dateRange.ToDateForDisplay,
            scope.WarehouseId,
            paging.Page,
            paging.PageSize,
            scope.ScopedWarehouseIds);

        var cachedReport = await TryGetCachedReportAsync<NxtReportDto>(
            reportName: "NXT",
            cacheKey,
            cancellationToken);

        if (cachedReport is not null)
        {
            return cachedReport;
        }

        var reportFromDatabase = await BuildNxtReportFromDatabaseAsync(
            dateRange,
            scope.WarehouseId,
            scope.ScopedWarehouseIds,
            paging.Skip,
            paging.Take,
            paging.Page,
            paging.PageSize,
            cancellationToken);

        var ttlMinutes = dateRange.IncludesToday()
            ? _cacheOptions.ReportCurrentPeriodTtlMinutes
            : _cacheOptions.ReportTtlMinutes;

        await StoreReportInCacheAsync(
            reportName: "NXT",
            cacheKey,
            reportFromDatabase,
            ttl: TimeSpan.FromMinutes(ttlMinutes),
            cancellationToken);

        return reportFromDatabase;
    }

    public async Task<PagedResultDto<ProductCostHistoryDto>> GetCostHistoryAsync(
        Guid? productVariantId = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _productCostHistoryRepository.GetPagedListAsync(
            productVariantId, skip, take, cancellationToken);

        return PagingNormalizer.Create(
            items.Select(x => new ProductCostHistoryDto
            {
                Id = x.Id,
                ProductVariantId = x.ProductVariantId,
                Sku = x.ProductVariant?.Sku ?? string.Empty,
                CostPrice = x.CostPrice,
                CostSource = x.CostSource,
                ValuationMethod = x.ValuationMethod,
                Currency = x.Currency,
                ReferenceType = x.ReferenceType,
                ReferenceId = x.ReferenceId,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                IsCurrent = x.IsCurrent
            }).ToList(),
            totalCount,
            normalizedPage,
            normalizedPageSize);
    }

    public async Task<List<NearExpiryLotDto>> GetNearExpiryLotsAsync(
        int daysAhead = 30,
        Guid? warehouseId = null,
        Guid? brandId = null,
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(warehouseId);
        var expiryBefore = DateTime.UtcNow.Date.AddDays(daysAhead);
        var lots = await _stockLotRepository.GetNearExpiryAsync(
            expiryBefore,
            scope.WarehouseId,
            brandId,
            200,
            scope.ScopedWarehouseIds,
            cancellationToken);

        var result = new List<NearExpiryLotDto>();
        foreach (var lot in lots)
        {
            foreach (var lotStock in lot.LotStocks.Where(x => x.QuantityOnHand > 0))
            {
                if (scope.WarehouseId.HasValue && lotStock.WarehouseId != scope.WarehouseId.Value)
                {
                    continue;
                }

                if (scope.ScopedWarehouseIds is { Count: > 0 }
                    && !scope.ScopedWarehouseIds.Contains(lotStock.WarehouseId))
                {
                    continue;
                }

                var daysUntil = lot.ExpiryDate.HasValue
                    ? (int)(lot.ExpiryDate.Value.Date - DateTime.UtcNow.Date).TotalDays
                    : int.MaxValue;

                result.Add(new NearExpiryLotDto
                {
                    StockLotId = lot.Id,
                    LotCode = lot.LotCode,
                    ProductVariantId = lot.ProductVariantId,
                    Sku = lot.ProductVariant?.Sku ?? string.Empty,
                    WarehouseId = lotStock.WarehouseId,
                    WarehouseCode = lotStock.Warehouse?.Code ?? string.Empty,
                    QuantityOnHand = lotStock.QuantityOnHand,
                    ExpiryDate = lot.ExpiryDate,
                    DaysUntilExpiry = daysUntil
                });
            }
        }

        return result.OrderBy(x => x.ExpiryDate ?? DateTime.MaxValue).ToList();
    }

    public async Task<PagedResultDto<LotStockDto>> GetLotStocksAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(warehouseId);
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _lotStockRepository.GetPagedListAsync(
            scope.WarehouseId,
            productVariantId,
            skip,
            take,
            scope.ScopedWarehouseIds,
            cancellationToken);

        return PagingNormalizer.Create(
            items.Select(x => new LotStockDto
            {
                Id = x.Id,
                StockLotId = x.StockLotId,
                LotCode = x.StockLot?.LotCode ?? string.Empty,
                ProductVariantId = x.StockLot?.ProductVariantId ?? Guid.Empty,
                Sku = x.StockLot?.ProductVariant?.Sku ?? string.Empty,
                WarehouseId = x.WarehouseId,
                WarehouseCode = x.Warehouse?.Code ?? string.Empty,
                QuantityOnHand = x.QuantityOnHand,
                ExpiryDate = x.StockLot?.ExpiryDate
            }).ToList(),
            totalCount,
            normalizedPage,
            normalizedPageSize);
    }

    private async Task<T?> TryGetCachedReportAsync<T>(
        string reportName,
        string cacheKey,
        CancellationToken cancellationToken)
        where T : class
    {
        var cached = await _cacheService.GetAsync<T>(cacheKey, cancellationToken);
        if (cached is null)
        {
            if (_cacheOptions.LogOperations)
            {
                _logger.LogDebug(
                    "{Scope} {ReportName} cache miss — will query database. key={CacheKey}",
                    LogScope,
                    reportName,
                    cacheKey);
            }

            return null;
        }

        if (_cacheOptions.LogOperations)
        {
            _logger.LogDebug(
                "{Scope} {ReportName} cache hit. key={CacheKey}",
                LogScope,
                reportName,
                cacheKey);
        }

        return cached;
    }

    private async Task StoreReportInCacheAsync<T>(
        string reportName,
        string cacheKey,
        T report,
        TimeSpan ttl,
        CancellationToken cancellationToken)
        where T : class
    {
        await _cacheService.SetAsync(cacheKey, report, ttl, cancellationToken);

        if (_cacheOptions.LogOperations)
        {
            _logger.LogDebug(
                "{Scope} {ReportName} stored in cache. key={CacheKey}, ttlMinutes={TtlMinutes}",
                LogScope,
                reportName,
                cacheKey,
                ttl.TotalMinutes);
        }
    }

    private async Task<InventoryValueReportDto> BuildInventoryValueReportFromDatabaseAsync(
        Guid? warehouseId,
        Guid? brandId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        int skip,
        int take,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "{Scope} Building inventory value report from database. warehouseId={WarehouseId}, brandId={BrandId}, page={Page}, pageSize={PageSize}",
            LogScope,
            warehouseId,
            brandId,
            page,
            pageSize);

        var (totalValue, totalLineCount) = await _inventoryReportReadRepository.GetInventoryValueTotalsAsync(
            warehouseId,
            brandId,
            scopedWarehouseIds,
            cancellationToken);

        var linesFromDatabase = await _inventoryReportReadRepository.GetInventoryValueLinesAsync(
            warehouseId,
            brandId,
            skip,
            take,
            scopedWarehouseIds,
            cancellationToken);

        return new InventoryValueReportDto
        {
            TotalValue = totalValue,
            TotalLineCount = totalLineCount,
            Page = page,
            PageSize = pageSize,
            Lines = linesFromDatabase.Select(MapInventoryValueLine).ToList()
        };
    }

    private async Task<NxtReportDto> BuildNxtReportFromDatabaseAsync(
        ReportDateRange dateRange,
        Guid? warehouseId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        int skip,
        int take,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "{Scope} Building NXT report from database. from={FromInclusive}, toExclusive={ToExclusive}, warehouseId={WarehouseId}, page={Page}, pageSize={PageSize}",
            LogScope,
            dateRange.FromInclusiveUtc,
            dateRange.ToExclusiveUtc,
            warehouseId,
            page,
            pageSize);

        var totals = await _inventoryReportReadRepository.GetNxtTotalsAsync(
            dateRange.FromInclusiveUtc,
            dateRange.ToExclusiveUtc,
            warehouseId,
            scopedWarehouseIds,
            cancellationToken);

        var linesFromDatabase = await _inventoryReportReadRepository.GetNxtLinesAsync(
            dateRange.FromInclusiveUtc,
            dateRange.ToExclusiveUtc,
            warehouseId,
            skip,
            take,
            scopedWarehouseIds,
            cancellationToken);

        return new NxtReportDto
        {
            FromDate = dateRange.FromInclusiveUtc,
            ToDate = dateRange.ToDateForDisplay,
            TotalOpeningValue = totals.TotalOpeningValue,
            TotalInValue = totals.TotalInValue,
            TotalOutValue = totals.TotalOutValue,
            TotalClosingValue = totals.TotalClosingValue,
            TotalLineCount = totals.TotalLineCount,
            Page = page,
            PageSize = pageSize,
            Lines = linesFromDatabase.Select(MapNxtLine).ToList()
        };
    }

    private static InventoryValueLineDto MapInventoryValueLine(InventoryValueLineReadModel line) => new()
    {
        ProductVariantId = line.ProductVariantId,
        Sku = line.Sku,
        WarehouseId = line.WarehouseId,
        WarehouseCode = line.WarehouseCode,
        QuantityOnHand = line.QuantityOnHand,
        UnitCost = line.UnitCost,
        InventoryValue = line.InventoryValue
    };

    private static NxtMovementLineDto MapNxtLine(NxtMovementLineReadModel line) => new()
    {
        ProductVariantId = line.ProductVariantId,
        Sku = line.Sku,
        WarehouseId = line.WarehouseId,
        WarehouseCode = line.WarehouseCode,
        OpeningQuantity = line.OpeningQuantity,
        InQuantity = line.InQuantity,
        OutQuantity = line.OutQuantity,
        ClosingQuantity = line.ClosingQuantity,
        UnitCost = line.UnitCost,
        OpeningValue = line.OpeningValue,
        InValue = line.InValue,
        OutValue = line.OutValue,
        ClosingValue = line.ClosingValue
    };
}
