using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API tra cứu sổ cái tồn kho (StockTransaction) — lịch sử mọi biến động tồn, dùng để audit và phân tích.
/// </summary>
[ApiController]
[Route("api/stock-transactions")]
public class StockTransactionsController : ControllerBase
{
    private readonly IStockTransactionAppService _stockTransactionAppService;

    public StockTransactionsController(IStockTransactionAppService stockTransactionAppService)
    {
        _stockTransactionAppService = stockTransactionAppService;
    }

    /// <summary>
    /// Lấy danh sách giao dịch tồn kho. Lọc theo warehouseId và/hoặc productVariantId (tùy chọn).
    /// </summary>
    [HttpGet]
    public Task<List<StockTransactionDto>> GetListAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        CancellationToken cancellationToken) =>
        _stockTransactionAppService.GetListAsync(warehouseId, productVariantId, cancellationToken);
}
