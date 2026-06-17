using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Services;

public interface IStockTransactionAppService
{
    Task<List<StockTransactionDto>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default);
}
