using StockLedgerRetail.Analytics;

namespace StockLedgerRetail.Services;

public interface IAnalyticsAppService
{
    Task<InventorySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<List<StockByWarehouseDto>> GetStockByWarehouseAsync(CancellationToken cancellationToken = default);

    Task<MovementSummaryDto> GetMovementSummaryAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<List<LowStockItemDto>> GetLowStockAsync(
        decimal threshold = 10,
        CancellationToken cancellationToken = default);
}
