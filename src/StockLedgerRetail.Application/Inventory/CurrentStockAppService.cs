using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

public class CurrentStockAppService : ICurrentStockAppService
{
    private readonly ICurrentStockRepository _currentStockRepository;

    public CurrentStockAppService(ICurrentStockRepository currentStockRepository)
    {
        _currentStockRepository = currentStockRepository;
    }

    public async Task<List<CurrentStockDto>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var stocks = await _currentStockRepository.GetListAsync(warehouseId, productVariantId, cancellationToken);
        return stocks.Select(MapToDto).ToList();
    }

    public async Task<CurrentStockDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stock = await _currentStockRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Current stock '{id}' was not found.");

        return MapToDto(stock);
    }

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
        LastTransactionId = stock.LastTransactionId,
        LastUpdatedAt = stock.LastUpdatedAt
    };
}
