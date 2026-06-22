using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Services;

public interface IInventoryInsightsAppService
{
    Task<List<DeadStockInsightDto>> GetDeadStockAsync(
        Guid? warehouseId = null,
        int daysWithoutOutbound = 60,
        decimal minOnHand = 1,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<List<SalesVelocityInsightDto>> GetSalesVelocityAsync(
        Guid? warehouseId = null,
        int lookbackDays = 30,
        int maxResults = 100,
        CancellationToken cancellationToken = default);

    Task<List<TransferSuggestionDto>> GetTransferSuggestionsAsync(
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        int lookbackDays = 30,
        int targetCoverDays = 14,
        int reserveCoverDays = 7,
        int maxResults = 20,
        CancellationToken cancellationToken = default);
}
