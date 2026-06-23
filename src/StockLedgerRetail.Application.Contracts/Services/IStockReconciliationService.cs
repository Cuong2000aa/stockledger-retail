using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Services;

public interface IStockReconciliationService
{
    Task<StockReconciliationResultDto> RunAsync(CancellationToken cancellationToken = default);
}
