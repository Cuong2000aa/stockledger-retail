using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Services;
using StockLedgerRetail.Warehouses;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseAppService _warehouseAppService;

    public WarehousesController(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    [HttpGet]
    public Task<List<WarehouseDto>> GetListAsync(CancellationToken cancellationToken) =>
        _warehouseAppService.GetListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<WarehouseDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _warehouseAppService.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<WarehouseDto> CreateAsync([FromBody] CreateWarehouseDto input, CancellationToken cancellationToken) =>
        _warehouseAppService.CreateAsync(input, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<WarehouseDto> UpdateAsync(Guid id, [FromBody] UpdateWarehouseDto input, CancellationToken cancellationToken) =>
        _warehouseAppService.UpdateAsync(id, input, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _warehouseAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
