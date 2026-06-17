using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Services;

public interface ICurrentStockAppService
{
    Task<List<CurrentStockDto>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default);

    Task<CurrentStockDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
