using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
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
    /// Lấy danh sách giao dịch tồn kho có phân trang. Lọc theo warehouseId và/hoặc productVariantId.
    /// </summary>
    [HttpGet]
    public Task<PagedResultDto<StockTransactionDto>> GetListAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _stockTransactionAppService.GetListAsync(warehouseId, productVariantId, page, pageSize, cancellationToken);
}
