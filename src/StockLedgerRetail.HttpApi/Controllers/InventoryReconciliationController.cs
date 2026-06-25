using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API đối soát tồn kho giữa sổ cái giao dịch và bảng tồn hiện tại.
/// Dùng để phát hiện lệch tồn do lỗi dữ liệu hoặc quy trình ghi sổ.
/// </summary>
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
