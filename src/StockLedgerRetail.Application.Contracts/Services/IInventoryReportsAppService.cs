using StockLedgerRetail.Common;
using StockLedgerRetail.Reports;

namespace StockLedgerRetail.Services;

public interface IInventoryReportsAppService
{
    Task<InventoryValueReportDto> GetInventoryValueAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        CancellationToken cancellationToken = default);

    Task<NxtReportDto> GetNxtReportAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProductCostHistoryDto>> GetCostHistoryAsync(
        Guid? productVariantId = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<List<NearExpiryLotDto>> GetNearExpiryLotsAsync(
        int daysAhead = 30,
        Guid? warehouseId = null,
        Guid? brandId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<LotStockDto>> GetLotStocksAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}
