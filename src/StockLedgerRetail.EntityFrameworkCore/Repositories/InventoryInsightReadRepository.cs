using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class InventoryInsightReadRepository : IInventoryInsightReadRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public InventoryInsightReadRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<DeadStockFact>> GetDeadStockFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default) =>
        GetDeadStockFactsCoreAsync(
            warehouseId,
            brandId,
            regionCode,
            referenceDateUtc,
            daysWithoutOutbound,
            minOnHand,
            maxResults,
            scopedWarehouseIds,
            cancellationToken);

    public Task<List<SalesVelocityFact>> GetSalesVelocityFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default) =>
        GetSalesVelocityFactsCoreAsync(
            warehouseId,
            brandId,
            regionCode,
            fromDateUtc,
            toDateUtc,
            maxResults,
            scopedWarehouseIds,
            cancellationToken);

    public Task<List<MarkdownCandidateFact>> GetMarkdownCandidateFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default) =>
        GetMarkdownCandidateFactsCoreAsync(
            warehouseId,
            brandId,
            regionCode,
            referenceDateUtc,
            daysWithoutOutbound,
            minOnHand,
            maxResults,
            scopedWarehouseIds,
            cancellationToken);

    public Task<List<PromotionRiskFact>> GetPromotionRiskFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default) =>
        GetPromotionRiskFactsCoreAsync(
            warehouseId,
            brandId,
            regionCode,
            fromDateUtc,
            toDateUtc,
            maxResults,
            scopedWarehouseIds,
            cancellationToken);

    public Task<List<ReorderRiskFact>> GetReorderRiskFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default) =>
        GetReorderRiskFactsCoreAsync(
            warehouseId,
            brandId,
            regionCode,
            fromDateUtc,
            toDateUtc,
            maxResults,
            scopedWarehouseIds,
            cancellationToken);

    public Task<List<TrendSummaryFact>> GetTrendSummaryFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime currentFromDateUtc,
        DateTime currentToDateUtc,
        DateTime previousFromDateUtc,
        DateTime previousToDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default) =>
        GetTrendSummaryFactsCoreAsync(
            warehouseId,
            brandId,
            regionCode,
            currentFromDateUtc,
            currentToDateUtc,
            previousFromDateUtc,
            previousToDateUtc,
            maxResults,
            scopedWarehouseIds,
            cancellationToken);

    private async Task<List<DeadStockFact>> GetDeadStockFactsCoreAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        var cutoffDate = referenceDateUtc.Date.AddDays(-daysWithoutOutbound);
        var stocks = await LoadStockRowsAsync(warehouseId, brandId, regionCode, scopedWarehouseIds, cancellationToken);
        var filteredStocks = stocks
            .Where(x => x.QuantityOnHand >= minOnHand)
            .ToList();
        var outboundFacts = await LoadLastOutboundAsync(filteredStocks, cancellationToken);

        return filteredStocks
            .Select(x => new DeadStockFact
            {
                ProductVariantId = x.ProductVariantId,
                Sku = x.Sku,
                WarehouseId = x.WarehouseId,
                BrandId = x.BrandId,
                RegionCode = x.RegionCode,
                WarehouseType = x.WarehouseType,
                WarehouseCode = x.WarehouseCode,
                WarehouseName = x.WarehouseName,
                QuantityOnHand = x.QuantityOnHand,
                QuantityAvailable = x.QuantityAvailable,
                CostPrice = x.CostPrice,
                CurrentSellingPriceBeforeVat = x.CurrentSellingPriceBeforeVat,
                CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                VatRate = x.VatRate,
                InventoryValue = x.InventoryValue,
                LastOutboundAt = outboundFacts.GetValueOrDefault(new InsightKey(x.ProductVariantId, x.WarehouseId))
            })
            .Where(x => !x.LastOutboundAt.HasValue || x.LastOutboundAt.Value <= cutoffDate)
            .OrderBy(x => x.LastOutboundAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.QuantityOnHand)
            .Take(maxResults)
            .ToList();
    }

    private async Task<List<SalesVelocityFact>> GetSalesVelocityFactsCoreAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        var stocks = await LoadStockRowsAsync(warehouseId, brandId, regionCode, scopedWarehouseIds, cancellationToken);
        var filteredStocks = stocks
            .Where(x => x.WarehouseType != WarehouseType.InTransit)
            .ToList();
        var outboundFacts = await LoadOutboundWindowAsync(filteredStocks, fromDateUtc, toDateUtc, cancellationToken);

        return filteredStocks
            .Select(x =>
            {
                outboundFacts.TryGetValue(new InsightKey(x.ProductVariantId, x.WarehouseId), out var outbound);
                return new SalesVelocityFact
                {
                    ProductVariantId = x.ProductVariantId,
                    Sku = x.Sku,
                    WarehouseId = x.WarehouseId,
                    BrandId = x.BrandId,
                    RegionCode = x.RegionCode,
                    WarehouseType = x.WarehouseType,
                    WarehouseCode = x.WarehouseCode,
                    WarehouseName = x.WarehouseName,
                    QuantityOnHand = x.QuantityOnHand,
                    QuantityAvailable = x.QuantityAvailable,
                    OutboundQuantity = outbound?.OutboundQuantity ?? 0,
                    CostPrice = x.CostPrice,
                    SellingPrice = x.CurrentSellingPriceAfterVat,
                    CurrentSellingPriceBeforeVat = x.CurrentSellingPriceBeforeVat,
                    CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                    VatRate = x.VatRate,
                    InventoryValue = x.InventoryValue,
                    LastOutboundAt = outbound?.LastOutboundAt
                };
            })
            .OrderByDescending(x => x.OutboundQuantity)
            .ThenBy(x => x.QuantityAvailable)
            .Take(maxResults)
            .ToList();
    }

    private async Task<List<MarkdownCandidateFact>> GetMarkdownCandidateFactsCoreAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        var cutoffDate = referenceDateUtc.Date.AddDays(-daysWithoutOutbound);
        var stocks = await LoadStockRowsAsync(warehouseId, brandId, regionCode, scopedWarehouseIds, cancellationToken);
        var filteredStocks = stocks
            .Where(x =>
                x.QuantityOnHand >= minOnHand
                && x.WarehouseType != WarehouseType.InTransit
                && x.CurrentSellingPriceBeforeVat.HasValue
                && x.CostPrice.HasValue)
            .ToList();
        var outboundFacts = await LoadLastOutboundAsync(filteredStocks, cancellationToken);

        return filteredStocks
            .Select(x => new MarkdownCandidateFact
            {
                ProductVariantId = x.ProductVariantId,
                Sku = x.Sku,
                WarehouseId = x.WarehouseId,
                BrandId = x.BrandId,
                RegionCode = x.RegionCode,
                WarehouseType = x.WarehouseType,
                WarehouseCode = x.WarehouseCode,
                WarehouseName = x.WarehouseName,
                QuantityOnHand = x.QuantityOnHand,
                QuantityAvailable = x.QuantityAvailable,
                CostPrice = x.CostPrice,
                CurrentSellingPriceBeforeVat = x.CurrentSellingPriceBeforeVat,
                CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                VatRate = x.VatRate,
                InventoryValue = x.InventoryValue,
                LastOutboundAt = outboundFacts.GetValueOrDefault(new InsightKey(x.ProductVariantId, x.WarehouseId))
            })
            .Where(x => !x.LastOutboundAt.HasValue || x.LastOutboundAt.Value <= cutoffDate)
            .OrderByDescending(x => x.InventoryValue ?? 0)
            .ThenByDescending(x => x.QuantityOnHand)
            .Take(maxResults)
            .ToList();
    }

    private async Task<List<PromotionRiskFact>> GetPromotionRiskFactsCoreAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        var stocks = await LoadStockRowsAsync(warehouseId, brandId, regionCode, scopedWarehouseIds, cancellationToken);
        var filteredStocks = stocks
            .Where(x => x.WarehouseType != WarehouseType.InTransit)
            .ToList();
        var outboundFacts = await LoadOutboundWindowAsync(filteredStocks, fromDateUtc, toDateUtc, cancellationToken);
        var promoPrices = await LoadCurrentPricesAsync(PriceType.Promotion, cancellationToken);

        return filteredStocks
            .Select(x =>
            {
                outboundFacts.TryGetValue(new InsightKey(x.ProductVariantId, x.WarehouseId), out var outbound);
                promoPrices.TryGetValue(x.ProductVariantId, out var promoPrice);
                return new PromotionRiskFact
                {
                    ProductVariantId = x.ProductVariantId,
                    Sku = x.Sku,
                    WarehouseId = x.WarehouseId,
                    BrandId = x.BrandId,
                    RegionCode = x.RegionCode,
                    WarehouseType = x.WarehouseType,
                    WarehouseCode = x.WarehouseCode,
                    WarehouseName = x.WarehouseName,
                    QuantityOnHand = x.QuantityOnHand,
                    QuantityAvailable = x.QuantityAvailable,
                    OutboundQuantity = outbound?.OutboundQuantity ?? 0,
                    CostPrice = x.CostPrice,
                    RegularPriceBeforeVat = x.CurrentSellingPriceBeforeVat,
                    RegularPriceAfterVat = x.CurrentSellingPriceAfterVat,
                    PromotionPriceBeforeVat = promoPrice?.PriceBeforeVat,
                    PromotionPriceAfterVat = promoPrice?.PriceAfterVat,
                    VatRate = promoPrice?.VatRate ?? x.VatRate,
                    LastOutboundAt = outbound?.LastOutboundAt
                };
            })
            .Where(x => x.PromotionPriceAfterVat.HasValue)
            .OrderByDescending(x => x.OutboundQuantity)
            .ThenBy(x => x.QuantityAvailable)
            .Take(maxResults)
            .ToList();
    }

    private async Task<List<ReorderRiskFact>> GetReorderRiskFactsCoreAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        var stocks = await LoadStockRowsAsync(warehouseId, brandId, regionCode, scopedWarehouseIds, cancellationToken);
        var filteredStocks = stocks
            .Where(x => x.WarehouseType != WarehouseType.InTransit)
            .ToList();
        var outboundFacts = await LoadOutboundWindowAsync(filteredStocks, fromDateUtc, toDateUtc, cancellationToken);
        var pipeline = await LoadPurchasePipelineAsync(filteredStocks, cancellationToken);

        return filteredStocks
            .Select(x =>
            {
                outboundFacts.TryGetValue(new InsightKey(x.ProductVariantId, x.WarehouseId), out var outbound);
                pipeline.TryGetValue(new InsightKey(x.ProductVariantId, x.WarehouseId), out var flow);
                return new ReorderRiskFact
                {
                    ProductVariantId = x.ProductVariantId,
                    Sku = x.Sku,
                    WarehouseId = x.WarehouseId,
                    BrandId = x.BrandId,
                    RegionCode = x.RegionCode,
                    WarehouseType = x.WarehouseType,
                    WarehouseCode = x.WarehouseCode,
                    WarehouseName = x.WarehouseName,
                    QuantityOnHand = x.QuantityOnHand,
                    QuantityAvailable = x.QuantityAvailable,
                    OutboundQuantity = outbound?.OutboundQuantity ?? 0,
                    QuantityOnOrder = flow?.QuantityOnOrder ?? 0,
                    QuantityInReceiving = flow?.QuantityInReceiving ?? 0,
                    CostPrice = x.CostPrice,
                    CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                    LastOutboundAt = outbound?.LastOutboundAt
                };
            })
            .Where(x => x.OutboundQuantity > 0 || x.QuantityAvailable > 0)
            .OrderByDescending(x => x.OutboundQuantity)
            .ThenBy(x => x.QuantityAvailable)
            .Take(maxResults)
            .ToList();
    }

    private async Task<List<TrendSummaryFact>> GetTrendSummaryFactsCoreAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime currentFromDateUtc,
        DateTime currentToDateUtc,
        DateTime previousFromDateUtc,
        DateTime previousToDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        var stocks = await LoadStockRowsAsync(warehouseId, brandId, regionCode, scopedWarehouseIds, cancellationToken);
        var filteredStocks = stocks
            .Where(x => x.WarehouseType != WarehouseType.InTransit)
            .ToList();
        var currentOutbound = await LoadOutboundWindowAsync(filteredStocks, currentFromDateUtc, currentToDateUtc, cancellationToken);
        var previousOutbound = await LoadOutboundWindowAsync(filteredStocks, previousFromDateUtc, previousToDateUtc, cancellationToken);
        var previousInventoryValues = await LoadHistoricalInventoryValuesAsync(filteredStocks, previousToDateUtc, cancellationToken);
        var previousRegularPrices = await LoadHistoricalPriceValuesAsync(filteredStocks, previousToDateUtc, cancellationToken);

        return filteredStocks
            .Select(x =>
            {
                currentOutbound.TryGetValue(new InsightKey(x.ProductVariantId, x.WarehouseId), out var current);
                previousOutbound.TryGetValue(new InsightKey(x.ProductVariantId, x.WarehouseId), out var previous);
                previousInventoryValues.TryGetValue(new InsightKey(x.ProductVariantId, x.WarehouseId), out var previousInventoryValue);
                previousRegularPrices.TryGetValue(x.ProductVariantId, out var previousPrice);
                return new TrendSummaryFact
                {
                    ProductVariantId = x.ProductVariantId,
                    Sku = x.Sku,
                    WarehouseId = x.WarehouseId,
                    BrandId = x.BrandId,
                    RegionCode = x.RegionCode,
                    WarehouseType = x.WarehouseType,
                    WarehouseCode = x.WarehouseCode,
                    WarehouseName = x.WarehouseName,
                    CurrentQuantityOnHand = x.QuantityOnHand,
                    CurrentInventoryValue = x.InventoryValue ?? 0,
                    PreviousInventoryValue = previousInventoryValue,
                    CurrentOutboundQuantity = current?.OutboundQuantity ?? 0,
                    PreviousOutboundQuantity = previous?.OutboundQuantity ?? 0,
                    CurrentSellingPriceAfterVat = x.CurrentSellingPriceAfterVat,
                    PreviousSellingPriceAfterVat = previousPrice
                };
            })
            .OrderByDescending(x => Math.Abs(x.CurrentOutboundQuantity - x.PreviousOutboundQuantity))
            .ThenByDescending(x => Math.Abs(x.CurrentInventoryValue - x.PreviousInventoryValue))
            .Take(maxResults)
            .ToList();
    }

    private async Task<List<InsightStockRow>> LoadStockRowsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        var normalizedRegion = NormalizeRegion(regionCode);
        var rows = await (
            from stock in _dbContext.CurrentStocks.AsNoTracking()
            join productVariant in _dbContext.ProductVariants.AsNoTracking()
                on stock.ProductVariantId equals productVariant.Id
            join product in _dbContext.Products.AsNoTracking()
                on productVariant.ProductId equals product.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on stock.WarehouseId equals warehouse.Id
            where !warehouseId.HasValue || stock.WarehouseId == warehouseId.Value
            where scopedWarehouseIds == null || scopedWarehouseIds.Contains(stock.WarehouseId)
            where !brandId.HasValue
                || warehouse.BrandId == brandId
                || productVariant.BrandId == brandId
                || product.BrandId == brandId
            where normalizedRegion == null
                || warehouse.RegionCode == null
                || warehouse.RegionCode.ToUpper() == normalizedRegion
            select new InsightStockRow
            {
                ProductVariantId = stock.ProductVariantId,
                WarehouseId = stock.WarehouseId,
                Sku = productVariant.Sku,
                BrandId = warehouse.BrandId ?? productVariant.BrandId ?? product.BrandId,
                RegionCode = warehouse.RegionCode,
                WarehouseType = warehouse.Type,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                QuantityOnHand = stock.QuantityOnHand,
                QuantityAvailable = stock.QuantityAvailable,
                CostPrice = productVariant.CurrentCostPrice ?? productVariant.CostPrice,
                CurrentSellingPriceBeforeVat = productVariant.CurrentSellingPriceBeforeVat,
                CurrentSellingPriceAfterVat = productVariant.CurrentSellingPriceAfterVat ?? productVariant.CurrentSellingPrice ?? productVariant.SellingPrice,
                VatRate = productVariant.VatRate
            })
            .ToListAsync(cancellationToken);

        var inventoryValues = await LoadLatestInventoryValuesAsync(rows, cancellationToken);
        foreach (var row in rows)
        {
            if (!inventoryValues.TryGetValue(new InsightKey(row.ProductVariantId, row.WarehouseId), out var inventoryValue))
            {
                inventoryValue = (row.CostPrice ?? 0) * row.QuantityOnHand;
            }

            row.InventoryValue = inventoryValue;
        }

        return rows;
    }

    private async Task<Dictionary<InsightKey, DateTime?>> LoadLastOutboundAsync(
        List<InsightStockRow> stocks,
        CancellationToken cancellationToken)
    {
        var variantIds = stocks.Select(x => x.ProductVariantId).Distinct().ToList();
        var warehouseIds = stocks.Select(x => x.WarehouseId).Distinct().ToList();

        return await _dbContext.StockTransactions
            .AsNoTracking()
            .Where(x =>
                x.TransactionType == StockTransactionType.Out
                && variantIds.Contains(x.ProductVariantId)
                && warehouseIds.Contains(x.WarehouseId))
            .GroupBy(x => new { x.ProductVariantId, x.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductVariantId,
                g.Key.WarehouseId,
                LastOutboundAt = g.Max(x => (DateTime?)x.TransactionDate)
            })
            .ToDictionaryAsync(
                x => new InsightKey(x.ProductVariantId, x.WarehouseId),
                x => x.LastOutboundAt,
                cancellationToken);
    }

    private async Task<Dictionary<InsightKey, OutboundWindowStat>> LoadOutboundWindowAsync(
        List<InsightStockRow> stocks,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken)
    {
        var variantIds = stocks.Select(x => x.ProductVariantId).Distinct().ToList();
        var warehouseIds = stocks.Select(x => x.WarehouseId).Distinct().ToList();

        return await _dbContext.StockTransactions
            .AsNoTracking()
            .Where(x =>
                x.TransactionType == StockTransactionType.Out
                && x.TransactionDate >= fromDateUtc
                && x.TransactionDate <= toDateUtc
                && variantIds.Contains(x.ProductVariantId)
                && warehouseIds.Contains(x.WarehouseId))
            .GroupBy(x => new { x.ProductVariantId, x.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductVariantId,
                g.Key.WarehouseId,
                OutboundQuantity = g.Sum(x => -x.QuantityDelta),
                LastOutboundAt = g.Max(x => (DateTime?)x.TransactionDate)
            })
            .ToDictionaryAsync(
                x => new InsightKey(x.ProductVariantId, x.WarehouseId),
                x => new OutboundWindowStat(x.OutboundQuantity, x.LastOutboundAt),
                cancellationToken);
    }

    private async Task<Dictionary<InsightKey, decimal>> LoadLatestInventoryValuesAsync(
        List<InsightStockRow> stocks,
        CancellationToken cancellationToken)
    {
        var variantIds = stocks.Select(x => x.ProductVariantId).Distinct().ToList();
        var warehouseIds = stocks.Select(x => x.WarehouseId).Distinct().ToList();

        var snapshots = await _dbContext.InventoryValuationSnapshots
            .AsNoTracking()
            .Where(x => variantIds.Contains(x.ProductVariantId) && warehouseIds.Contains(x.WarehouseId))
            .OrderByDescending(x => x.SnapshotDate)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(x => new InsightKey(x.ProductVariantId, x.WarehouseId))
            .ToDictionary(g => g.Key, g => g.First().InventoryValue);
    }

    private async Task<Dictionary<InsightKey, decimal>> LoadHistoricalInventoryValuesAsync(
        List<InsightStockRow> stocks,
        DateTime snapshotDateUtc,
        CancellationToken cancellationToken)
    {
        var variantIds = stocks.Select(x => x.ProductVariantId).Distinct().ToList();
        var warehouseIds = stocks.Select(x => x.WarehouseId).Distinct().ToList();

        var snapshots = await _dbContext.InventoryValuationSnapshots
            .AsNoTracking()
            .Where(x =>
                variantIds.Contains(x.ProductVariantId)
                && warehouseIds.Contains(x.WarehouseId)
                && x.SnapshotDate <= snapshotDateUtc)
            .OrderByDescending(x => x.SnapshotDate)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(x => new InsightKey(x.ProductVariantId, x.WarehouseId))
            .ToDictionary(g => g.Key, g => g.First().InventoryValue);
    }

    private async Task<Dictionary<Guid, ProductPrice>> LoadCurrentPricesAsync(
        PriceType priceType,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductPrices
            .AsNoTracking()
            .Where(x => x.PriceType == priceType && x.IsCurrent)
            .GroupBy(x => x.ProductVariantId)
            .Select(g => g.OrderByDescending(x => x.EffectiveFrom).First())
            .ToDictionaryAsync(x => x.ProductVariantId, x => x, cancellationToken);
    }

    private async Task<Dictionary<Guid, decimal?>> LoadHistoricalPriceValuesAsync(
        List<InsightStockRow> stocks,
        DateTime atUtc,
        CancellationToken cancellationToken)
    {
        var variantIds = stocks.Select(x => x.ProductVariantId).Distinct().ToList();
        var prices = await _dbContext.ProductPrices
            .AsNoTracking()
            .Where(x =>
                x.PriceType == PriceType.Regular
                && variantIds.Contains(x.ProductVariantId)
                && x.EffectiveFrom <= atUtc
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= atUtc))
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

        return prices
            .GroupBy(x => x.ProductVariantId)
            .ToDictionary(g => g.Key, g => (decimal?)g.First().PriceAfterVat);
    }

    private async Task<Dictionary<InsightKey, PurchasePipelineStat>> LoadPurchasePipelineAsync(
        List<InsightStockRow> stocks,
        CancellationToken cancellationToken)
    {
        var variantIds = stocks.Select(x => x.ProductVariantId).Distinct().ToList();
        var warehouseIds = stocks.Select(x => x.WarehouseId).Distinct().ToList();

        var poOpen = await (
            from line in _dbContext.PurchaseOrderLines.AsNoTracking()
            join po in _dbContext.PurchaseOrders.AsNoTracking() on line.PurchaseOrderId equals po.Id
            where variantIds.Contains(line.ProductVariantId)
            where warehouseIds.Contains(po.WarehouseId)
            where po.Status == PurchaseOrderStatus.Submitted
                || po.Status == PurchaseOrderStatus.PartiallyReceived
                || po.Status == PurchaseOrderStatus.PendingApproval
            group new { line, po } by new { line.ProductVariantId, po.WarehouseId } into g
            select new
            {
                g.Key.ProductVariantId,
                g.Key.WarehouseId,
                QuantityOnOrder = g.Sum(x => Math.Max(0, x.line.OrderedQuantity - x.line.ReceivedQuantity))
            })
            .ToListAsync(cancellationToken);

        var receiving = await (
            from line in _dbContext.GoodsReceiptLines.AsNoTracking()
            join gr in _dbContext.GoodsReceipts.AsNoTracking() on line.GoodsReceiptId equals gr.Id
            where variantIds.Contains(line.ProductVariantId)
            where warehouseIds.Contains(gr.WarehouseId)
            where gr.Status == GoodsReceiptStatus.Draft
            group line by new { line.ProductVariantId, gr.WarehouseId } into g
            select new
            {
                g.Key.ProductVariantId,
                g.Key.WarehouseId,
                QuantityInReceiving = g.Sum(x => x.ReceivedQuantity)
            })
            .ToListAsync(cancellationToken);

        var dict = new Dictionary<InsightKey, PurchasePipelineStat>();
        foreach (var item in poOpen)
        {
            dict[new InsightKey(item.ProductVariantId, item.WarehouseId)] = new PurchasePipelineStat(item.QuantityOnOrder, 0);
        }

        foreach (var item in receiving)
        {
            var key = new InsightKey(item.ProductVariantId, item.WarehouseId);
            if (dict.TryGetValue(key, out var current))
            {
                dict[key] = current with { QuantityInReceiving = item.QuantityInReceiving };
            }
            else
            {
                dict[key] = new PurchasePipelineStat(0, item.QuantityInReceiving);
            }
        }

        return dict;
    }

    private static string? NormalizeRegion(string? regionCode) =>
        string.IsNullOrWhiteSpace(regionCode) ? null : regionCode.Trim().ToUpperInvariant();

    private readonly record struct InsightKey(Guid ProductVariantId, Guid WarehouseId);

    private sealed class InsightStockRow
    {
        public Guid ProductVariantId { get; set; }
        public Guid WarehouseId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public Guid? BrandId { get; set; }
        public string? RegionCode { get; set; }
        public WarehouseType WarehouseType { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? CurrentSellingPriceBeforeVat { get; set; }
        public decimal? CurrentSellingPriceAfterVat { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? InventoryValue { get; set; }
    }

    private sealed record OutboundWindowStat(decimal OutboundQuantity, DateTime? LastOutboundAt);

    private sealed record PurchasePipelineStat(decimal QuantityOnOrder, decimal QuantityInReceiving);
}
