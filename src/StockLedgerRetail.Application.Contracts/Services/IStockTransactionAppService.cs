using StockLedgerRetail.Common;
using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Services;

public interface IStockTransactionAppService
{
    Task<PagedResultDto<StockTransactionDto>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default);
}
