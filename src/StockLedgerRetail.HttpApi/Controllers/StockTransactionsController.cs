using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/stock-transactions")]
public class StockTransactionsController : ControllerBase
{
    private readonly IStockTransactionAppService _stockTransactionAppService;

    public StockTransactionsController(IStockTransactionAppService stockTransactionAppService)
    {
        _stockTransactionAppService = stockTransactionAppService;
    }

    [HttpGet]
    public Task<List<StockTransactionDto>> GetListAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        CancellationToken cancellationToken) =>
        _stockTransactionAppService.GetListAsync(warehouseId, productVariantId, cancellationToken);
}
