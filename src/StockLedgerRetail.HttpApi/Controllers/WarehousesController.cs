using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Services;
using StockLedgerRetail.Warehouses;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API quản lý kho / cửa hàng / kho con — hỗ trợ cấu trúc phân cấp (parent warehouse).
/// </summary>
[ApiController]
[Route("api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseAppService _warehouseAppService;

    public WarehousesController(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    /// <summary>Lấy danh sách tất cả kho.</summary>
    [HttpGet]
    public Task<List<WarehouseDto>> GetListAsync(CancellationToken cancellationToken) =>
        _warehouseAppService.GetListAsync(cancellationToken);

    /// <summary>Lấy chi tiết một kho theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<WarehouseDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _warehouseAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo kho mới. Mã kho (Code) phải duy nhất.</summary>
    [HttpPost]
    public Task<WarehouseDto> CreateAsync([FromBody] CreateWarehouseDto input, CancellationToken cancellationToken) =>
        _warehouseAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật thông tin kho (tên, loại, kho cha, trạng thái).</summary>
    [HttpPut("{id:guid}")]
    public Task<WarehouseDto> UpdateAsync(Guid id, [FromBody] UpdateWarehouseDto input, CancellationToken cancellationToken) =>
        _warehouseAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Xóa kho theo Id.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _warehouseAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
