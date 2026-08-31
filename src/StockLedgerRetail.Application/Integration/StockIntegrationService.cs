using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Integration;

public class StockIntegrationService : IStockIntegrationService
{
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public StockIntegrationService(
        IStockTransactionRepository stockTransactionRepository,
        ICurrentStockRepository currentStockRepository,
        IStockReservationRepository stockReservationRepository,
        IProductVariantRepository productVariantRepository,
        IWarehouseRepository warehouseRepository)
    {
        _stockTransactionRepository = stockTransactionRepository;
        _currentStockRepository = currentStockRepository;
        _stockReservationRepository = stockReservationRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<StockDeltaResponseDto> GetStockDeltaAsync(
        StockDeltaQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(query.Limit, 1, 2000);
        var sinceUtc = query.SinceUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(query.SinceUtc, DateTimeKind.Utc)
            : query.SinceUtc.ToUniversalTime();

        // 1. Find all (ProductVariantId, WarehouseId) pairs active since timestamp
        var activePairs = await _stockTransactionRepository.GetActivePairsSinceAsync(
            sinceUtc,
            query.WarehouseId,
            cancellationToken: cancellationToken);

        if (activePairs.Count == 0)
        {
            return new StockDeltaResponseDto
            {
                SinceUtc = sinceUtc,
                GeneratedAtUtc = DateTime.UtcNow,
                TotalChanges = 0,
                HasMore = false,
                Items = []
            };
        }

        var totalChanges = activePairs.Count;
        var pagedPairs = activePairs.Take(limit).ToList();
        var hasMore = activePairs.Count > limit;

        // 2. Fetch current stock, reservations, variants, and warehouses for these pairs
        var variantIds = pagedPairs.Select(x => x.ProductVariantId).Distinct().ToList();
        var warehouseIds = pagedPairs.Select(x => x.WarehouseId).Distinct().ToList();

        var warehouses = (await _warehouseRepository.GetListAsync(cancellationToken))
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionary(w => w.Id);

        var variants = (await _productVariantRepository.GetListAsync(cancellationToken))
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionary(v => v.Id);

        if (query.BrandId.HasValue)
        {
            variants = variants
                .Where(v => v.Value.Product?.BrandId == query.BrandId.Value)
                .ToDictionary(k => k.Key, v => v.Value);
        }

        var currentStocks = await _currentStockRepository.GetListAsync(
            cancellationToken: cancellationToken);

        var stockDict = currentStocks
            .GroupBy(x => (x.ProductVariantId, x.WarehouseId))
            .ToDictionary(g => g.Key, g => g.First());

        var reservations = await _stockReservationRepository.GetActiveReservedQuantitiesAsync(
            variantIds,
            warehouseIds,
            cancellationToken);

        var items = new List<StockDeltaItemDto>();

        foreach (var pair in pagedPairs)
        {
            if (!variants.TryGetValue(pair.ProductVariantId, out var variant))
            {
                continue;
            }

            if (!warehouses.TryGetValue(pair.WarehouseId, out var warehouse))
            {
                continue;
            }

            var onHand = stockDict.TryGetValue((pair.ProductVariantId, pair.WarehouseId), out var stock)
                ? stock.QuantityOnHand
                : 0m;

            var reserved = reservations.TryGetValue((pair.ProductVariantId, pair.WarehouseId), out var resQty)
                ? resQty
                : 0m;

            var available = Math.Max(0m, onHand - reserved);

            items.Add(new StockDeltaItemDto
            {
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                ProductVariantId = variant.Id,
                Sku = variant.Sku,
                ProductName = variant.Product?.Name ?? string.Empty,
                Size = variant.Size,
                Color = variant.Color,
                OnHandQuantity = onHand,
                ReservedQuantity = reserved,
                AvailableQuantity = available,
                LastMovementAtUtc = stock?.LastUpdatedAt ?? DateTime.UtcNow
            });
        }

        return new StockDeltaResponseDto
        {
            SinceUtc = sinceUtc,
            GeneratedAtUtc = DateTime.UtcNow,
            TotalChanges = totalChanges,
            HasMore = hasMore,
            Items = items
        };
    }
}
