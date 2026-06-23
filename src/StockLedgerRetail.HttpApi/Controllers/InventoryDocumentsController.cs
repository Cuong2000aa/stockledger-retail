using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
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

    /// <summary>Lấy danh sách phiếu có phân trang. Có thể lọc theo loại phiếu (documentType).</summary>
    [HttpGet]
    public Task<PagedResultDto<InventoryDocumentDto>> GetListAsync(
        [FromQuery] InventoryDocumentType? documentType,
        [FromQuery] InventoryDocumentStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.GetListAsync(documentType, status, page, pageSize, search, cancellationToken);

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
    /// Tạo phiếu điều chỉnh tồn (Adjustment). Số lượng dòng có dấu: dương tăng, âm giảm.
    /// Bắt buộc nhập lý do (reason). Trạng thái ban đầu: Draft.
    /// </summary>
    [HttpPost("adjustment")]
    public Task<InventoryDocumentDto> CreateAdjustmentAsync(
        [FromBody] CreateAdjustmentDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CreateAdjustmentAsync(input, cancellationToken);

    /// <summary>
    /// Tạo phiếu chuyển kho (Transfer). Kho nguồn ≠ kho đích. Trạng thái ban đầu: Draft.
    /// </summary>
    [HttpPost("transfer")]
    public Task<InventoryDocumentDto> CreateTransferAsync(
        [FromBody] CreateTransferDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CreateTransferAsync(input, cancellationToken);

    /// <summary>
    /// Tạo phiếu kiểm kê (Stock Count). Dòng lưu số lượng kiểm thực tế; khi duyệt hệ thống tính chênh lệch với tồn.
    /// Trạng thái ban đầu: Draft.
    /// </summary>
    [HttpPost("stock-count")]
    public Task<InventoryDocumentDto> CreateStockCountAsync(
        [FromBody] CreateStockCountDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CreateStockCountAsync(input, cancellationToken);

    /// <summary>Cập nhật phiếu Draft — ngày, tham chiếu, ghi chú và/hoặc danh sách dòng hàng.</summary>
    [HttpPut("{id:guid}")]
    public Task<InventoryDocumentDto> UpdateDraftAsync(
        Guid id,
        [FromBody] UpdateInventoryDocumentDraftDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.UpdateDraftAsync(id, input, cancellationToken);

    /// <summary>
    /// Duyệt phiếu — sinh StockTransaction và cập nhật CurrentStock.
    /// Đây là bước thực sự làm thay đổi tồn kho.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public Task<InventoryDocumentDto> ApproveAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.ApproveAsync(id, cancellationToken);

    /// <summary>Nhận hàng chuyển kho tại kho đích (sau khi đã approve/ship).</summary>
    [HttpPost("{id:guid}/receive-transfer")]
    public Task<InventoryDocumentDto> ReceiveTransferAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.ReceiveTransferAsync(id, cancellationToken);

    /// <summary>Hủy phiếu Draft — chỉ áp dụng phiếu chưa duyệt, không tác động tồn kho.</summary>
    [HttpPost("{id:guid}/cancel")]
    public Task<InventoryDocumentDto> CancelAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CancelAsync(id, cancellationToken);
}
