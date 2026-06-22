using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Suppliers;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>API quản lý nhà cung cấp.</summary>
[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierAppService _supplierAppService;

    public SuppliersController(ISupplierAppService supplierAppService)
    {
        _supplierAppService = supplierAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<SupplierDto>> GetListAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        _supplierAppService.GetListAsync(page, pageSize, search, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<SupplierDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _supplierAppService.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<SupplierDto> CreateAsync([FromBody] CreateSupplierDto input, CancellationToken cancellationToken) =>
        _supplierAppService.CreateAsync(input, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<SupplierDto> UpdateAsync(Guid id, [FromBody] UpdateSupplierDto input, CancellationToken cancellationToken) =>
        _supplierAppService.UpdateAsync(id, input, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _supplierAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
