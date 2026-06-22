using StockLedgerRetail.Common;
using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Services;

public interface ICurrentStockAppService
{
    Task<PagedResultDto<CurrentStockDto>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<CurrentStockDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
