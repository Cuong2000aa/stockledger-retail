using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Reports;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Reports;

public class InventoryReportsAppService : IInventoryReportsAppService
{
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IProductCostHistoryRepository _productCostHistoryRepository;
    private readonly IStockLotRepository _stockLotRepository;
    private readonly ILotStockRepository _lotStockRepository;

    public InventoryReportsAppService(
        ICurrentStockRepository currentStockRepository,
        IStockTransactionRepository stockTransactionRepository,
        IProductCostHistoryRepository productCostHistoryRepository,
        IStockLotRepository stockLotRepository,
        ILotStockRepository lotStockRepository)
    {
        _currentStockRepository = currentStockRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _productCostHistoryRepository = productCostHistoryRepository;
        _stockLotRepository = stockLotRepository;
        _lotStockRepository = lotStockRepository;
    }

    public async Task<InventoryValueReportDto> GetInventoryValueAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        CancellationToken cancellationToken = default)
    {
        var stocks = await _currentStockRepository.GetListAsync(warehouseId, null, cancellationToken);
        var lines = stocks
            .Where(s => s.QuantityOnHand > 0)
            .Where(s => !brandId.HasValue || s.ProductVariant?.BrandId == brandId.Value)
            .Select(s =>
            {
                var unitCost = s.ProductVariant?.CostPrice ?? 0;
                return new InventoryValueLineDto
                {
                    ProductVariantId = s.ProductVariantId,
                    Sku = s.ProductVariant?.Sku ?? string.Empty,
                    WarehouseId = s.WarehouseId,
                    WarehouseCode = s.Warehouse?.Code ?? string.Empty,
                    QuantityOnHand = s.QuantityOnHand,
                    UnitCost = s.ProductVariant?.CostPrice,
                    InventoryValue = s.QuantityOnHand * unitCost
                };
            })
            .OrderByDescending(x => x.InventoryValue)
            .ToList();

        return new InventoryValueReportDto
        {
            TotalValue = lines.Sum(x => x.InventoryValue),
            Lines = lines
        };
    }

    public async Task<NxtReportDto> GetNxtReportAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _stockTransactionRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);
        if (warehouseId.HasValue)
        {
            transactions = transactions.Where(x => x.WarehouseId == warehouseId.Value).ToList();
        }

        var stocks = await _currentStockRepository.GetListAsync(warehouseId, null, cancellationToken);
        var keys = stocks
            .Select(s => (s.ProductVariantId, s.WarehouseId))
            .Union(transactions.Select(t => (t.ProductVariantId, t.WarehouseId)))
            .Distinct()
            .ToList();

        var lines = new List<NxtMovementLineDto>();
        foreach (var (variantId, whId) in keys)
        {
            var variantTransactions = transactions
                .Where(t => t.ProductVariantId == variantId && t.WarehouseId == whId)
                .ToList();

            var inQty = variantTransactions.Where(t => t.QuantityDelta > 0).Sum(t => t.QuantityDelta);
            var outQty = -variantTransactions.Where(t => t.QuantityDelta < 0).Sum(t => t.QuantityDelta);
            var closing = stocks.FirstOrDefault(s => s.ProductVariantId == variantId && s.WarehouseId == whId);
            var closingQty = closing?.QuantityOnHand ?? 0;
            var openingQty = closingQty - inQty + outQty;
            var unitCost = closing?.ProductVariant?.CostPrice ?? variantTransactions.FirstOrDefault()?.UnitCost;

            var cost = unitCost ?? 0;
            lines.Add(new NxtMovementLineDto
            {
                ProductVariantId = variantId,
                Sku = closing?.ProductVariant?.Sku
                    ?? variantTransactions.FirstOrDefault()?.ProductVariant?.Sku
                    ?? string.Empty,
                WarehouseId = whId,
                WarehouseCode = closing?.Warehouse?.Code
                    ?? variantTransactions.FirstOrDefault()?.Warehouse?.Code
                    ?? string.Empty,
                OpeningQuantity = openingQty,
                InQuantity = inQty,
                OutQuantity = outQty,
                ClosingQuantity = closingQty,
                UnitCost = unitCost,
                OpeningValue = openingQty * cost,
                InValue = inQty * cost,
                OutValue = outQty * cost,
                ClosingValue = closingQty * cost
            });
        }

        return new NxtReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalOpeningValue = lines.Sum(x => x.OpeningValue),
            TotalInValue = lines.Sum(x => x.InValue),
            TotalOutValue = lines.Sum(x => x.OutValue),
            TotalClosingValue = lines.Sum(x => x.ClosingValue),
            Lines = lines.OrderByDescending(x => x.ClosingValue).ToList()
        };
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
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo
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
        var expiryBefore = DateTime.UtcNow.Date.AddDays(daysAhead);
        var lots = await _stockLotRepository.GetNearExpiryAsync(
            expiryBefore, warehouseId, brandId, 200, cancellationToken);

        var result = new List<NearExpiryLotDto>();
        foreach (var lot in lots)
        {
            foreach (var lotStock in lot.LotStocks.Where(x => x.QuantityOnHand > 0))
            {
                if (warehouseId.HasValue && lotStock.WarehouseId != warehouseId.Value)
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
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _lotStockRepository.GetPagedListAsync(
            warehouseId, productVariantId, skip, take, cancellationToken);

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
}
