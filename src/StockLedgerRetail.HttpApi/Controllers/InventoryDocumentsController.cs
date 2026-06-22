using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API quản lý phiếu nghiệp vụ tồn kho (nhập, xuất, chuyển, điều chỉnh...).
/// Phiếu ở trạng thái Draft; cần duyệt (approve) mới tác động tồn kho.
/// </summary>
[ApiController]
[Route("api/inventory-documents")]
public class InventoryDocumentsController : ControllerBase
{
    private readonly IInventoryDocumentAppService _inventoryDocumentAppService;

    public InventoryDocumentsController(IInventoryDocumentAppService inventoryDocumentAppService)
    {
        _inventoryDocumentAppService = inventoryDocumentAppService;
    }

    /// <summary>Lấy danh sách phiếu. Có thể lọc theo loại phiếu (documentType).</summary>
    [HttpGet]
    public Task<List<InventoryDocumentDto>> GetListAsync(
        [FromQuery] InventoryDocumentType? documentType,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.GetListAsync(documentType, cancellationToken);

    /// <summary>Lấy chi tiết phiếu kèm danh sách dòng hàng.</summary>
    [HttpGet("{id:guid}")]
    public Task<InventoryDocumentDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo phiếu nhập kho (Stock In). Trạng thái ban đầu: Draft — chưa tăng tồn.</summary>
    [HttpPost("stock-in")]
    public Task<InventoryDocumentDto> CreateStockInAsync(
        [FromBody] CreateStockInDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CreateStockInAsync(input, cancellationToken);

    /// <summary>Tạo phiếu xuất kho (Stock Out). Trạng thái ban đầu: Draft — chưa giảm tồn.</summary>
    [HttpPost("stock-out")]
    public Task<InventoryDocumentDto> CreateStockOutAsync(
        [FromBody] CreateStockOutDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CreateStockOutAsync(input, cancellationToken);

    /// <summary>
    /// Duyệt phiếu — sinh StockTransaction và cập nhật CurrentStock.
    /// Đây là bước thực sự làm thay đổi tồn kho.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public Task<InventoryDocumentDto> ApproveAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.ApproveAsync(id, cancellationToken);
}
