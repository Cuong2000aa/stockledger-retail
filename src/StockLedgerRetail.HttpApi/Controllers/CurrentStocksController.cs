using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/current-stocks")]
public class CurrentStocksController : ControllerBase
{
    private readonly ICurrentStockAppService _currentStockAppService;

    public CurrentStocksController(ICurrentStockAppService currentStockAppService)
    {
        _currentStockAppService = currentStockAppService;
    }

    [HttpGet]
    public Task<List<CurrentStockDto>> GetListAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        CancellationToken cancellationToken) =>
        _currentStockAppService.GetListAsync(warehouseId, productVariantId, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<CurrentStockDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _currentStockAppService.GetAsync(id, cancellationToken);
}
