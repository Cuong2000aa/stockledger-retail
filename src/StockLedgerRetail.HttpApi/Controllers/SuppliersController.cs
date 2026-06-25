using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Suppliers;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>API quản lý nhà cung cấp phục vụ luồng mua hàng và nhận hàng.</summary>
[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierAppService _supplierAppService;

    public SuppliersController(ISupplierAppService supplierAppService)
    {
        _supplierAppService = supplierAppService;
    }

    /// <summary>Lấy danh sách nhà cung cấp có phân trang và tìm kiếm theo từ khóa.</summary>
    [HttpGet]
    public Task<PagedResultDto<SupplierDto>> GetListAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        _supplierAppService.GetListAsync(page, pageSize, search, cancellationToken);

    /// <summary>Lấy chi tiết một nhà cung cấp theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<SupplierDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _supplierAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo nhà cung cấp mới.</summary>
    [HttpPost]
    public Task<SupplierDto> CreateAsync([FromBody] CreateSupplierDto input, CancellationToken cancellationToken) =>
        _supplierAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật thông tin nhà cung cấp theo Id.</summary>
    [HttpPut("{id:guid}")]
    public Task<SupplierDto> UpdateAsync(Guid id, [FromBody] UpdateSupplierDto input, CancellationToken cancellationToken) =>
        _supplierAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Xóa nhà cung cấp theo Id.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _supplierAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
