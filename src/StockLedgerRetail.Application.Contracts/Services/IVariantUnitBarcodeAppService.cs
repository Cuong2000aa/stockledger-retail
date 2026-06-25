using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Services;

public interface IVariantUnitBarcodeAppService
{
    Task<PagedResultDto<VariantUnitBarcodeDto>> GetListAsync(
        Guid productVariantId,
        Guid warehouseId,
        UnitBarcodeStatus? status = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default);
}
