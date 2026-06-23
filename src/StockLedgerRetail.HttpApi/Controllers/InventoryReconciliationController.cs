using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/inventory/reconciliation")]
public class InventoryReconciliationController : ControllerBase
{
    private readonly IStockReconciliationService _stockReconciliationService;

    public InventoryReconciliationController(IStockReconciliationService stockReconciliationService)
    {
        _stockReconciliationService = stockReconciliationService;
    }

    /// <summary>
    /// Chạy đối soát: SUM(stock_transactions.QuantityDelta) vs current_stocks.QuantityOnHand.
    /// </summary>
    [HttpPost("run")]
    public Task<StockReconciliationResultDto> RunAsync(CancellationToken cancellationToken) =>
        _stockReconciliationService.RunAsync(cancellationToken);
}
