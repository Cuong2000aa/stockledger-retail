using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API tra cứu tồn kho hiện tại theo SKU và kho — dùng cho POS kiểm tra còn hàng bán được không.
/// </summary>
[ApiController]
[Route("api/current-stocks")]
public class CurrentStocksController : ControllerBase
{
    private readonly ICurrentStockAppService _currentStockAppService;

    public CurrentStocksController(ICurrentStockAppService currentStockAppService)
    {
        _currentStockAppService = currentStockAppService;
    }

    /// <summary>
    /// Lấy danh sách tồn hiện tại có phân trang. Lọc theo warehouseId và/hoặc productVariantId.
    /// </summary>
    [HttpGet]
    public Task<PagedResultDto<CurrentStockDto>> GetListAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _currentStockAppService.GetListAsync(warehouseId, productVariantId, page, pageSize, cancellationToken);

    /// <summary>Lấy một bản ghi tồn hiện tại theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<CurrentStockDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _currentStockAppService.GetAsync(id, cancellationToken);
}
