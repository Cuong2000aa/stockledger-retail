using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>
/// Dịch vụ tra cứu tồn kho hiện tại (CurrentStock) — snapshot sau giao dịch gần nhất.
/// </summary>
public class CurrentStockAppService : ICurrentStockAppService
{
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IWarehouseScopeService _warehouseScopeService;

    public CurrentStockAppService(
        ICurrentStockRepository currentStockRepository,
        IWarehouseScopeService warehouseScopeService)
    {
        _currentStockRepository = currentStockRepository;
        _warehouseScopeService = warehouseScopeService;
    }

    /// <summary>Lấy danh sách tồn, lọc theo kho và/hoặc SKU (tùy chọn).</summary>
    public async Task<PagedResultDto<CurrentStockDto>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var scopedWarehouseId = _warehouseScopeService.NormalizeWarehouseFilter(warehouseId);
        var scopedWarehouseIds = scopedWarehouseId.HasValue
            ? null
            : _warehouseScopeService.GetWarehouseFilterForLists();

        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (stocks, totalCount) = await _currentStockRepository.GetPagedListAsync(
            scopedWarehouseId,
            productVariantId,
            skip,
            take,
            search,
            scopedWarehouseIds,
            cancellationToken);
        var items = stocks.Select(MapToDto).ToList();
        return PagingNormalizer.Create(items, totalCount, normalizedPage, normalizedPageSize);
    }

    /// <summary>Lấy một bản ghi tồn theo Id.</summary>
    public async Task<CurrentStockDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stock = await _currentStockRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Current stock '{id}' was not found.");

        _warehouseScopeService.EnsureWarehouseAccess(stock.WarehouseId);

        return MapToDto(stock);
    }

    /// <summary>Chuyển entity CurrentStock sang DTO kèm mã SKU và thông tin kho.</summary>
    private static CurrentStockDto MapToDto(Domain.Entities.CurrentStock stock) => new()
    {
        Id = stock.Id,
        ProductVariantId = stock.ProductVariantId,
        Sku = stock.ProductVariant.Sku,
        WarehouseId = stock.WarehouseId,
        WarehouseCode = stock.Warehouse.Code,
        WarehouseName = stock.Warehouse.Name,
        QuantityOnHand = stock.QuantityOnHand,
        QuantityReserved = stock.QuantityReserved,
        QuantityAvailable = stock.QuantityAvailable,
        IsBarcode = stock.ProductVariant.IsBarcode,
        LastTransactionId = stock.LastTransactionId,
        LastUpdatedAt = stock.LastUpdatedAt
    };
}
