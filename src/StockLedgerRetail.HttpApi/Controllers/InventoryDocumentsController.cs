using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API quản lý phiếu nghiệp vụ tồn kho (nhập, xuất, chuyển, điều chỉnh, kiểm kê).
/// Phiếu tạo ở trạng thái Draft; chỉ khi duyệt (approve) mới sinh StockTransaction và cập nhật CurrentStock.
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

    /// <summary>Lấy danh sách phiếu tồn kho có phân trang. Lọc theo loại phiếu (documentType), trạng thái (status), từ khóa tìm kiếm.</summary>
    [HttpGet]
    public Task<PagedResultDto<InventoryDocumentDto>> GetListAsync(
        [FromQuery] InventoryDocumentType? documentType,
        [FromQuery] InventoryDocumentStatus? status,
        [FromQuery] TransferLifecycleStatus? transferLifecycle,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.GetListAsync(
            documentType,
            status,
            transferLifecycle,
            page,
            pageSize,
            search,
            cancellationToken);

    /// <summary>Lấy chi tiết một phiếu theo Id, kèm đầy đủ danh sách dòng hàng (SKU, số lượng, kho...).</summary>
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

    /// <summary>Cập nhật phiếu đang ở trạng thái Draft — sửa ngày, mã tham chiếu, ghi chú và/hoặc thay đổi danh sách dòng hàng. Phiếu đã duyệt không sửa được.</summary>
    [HttpPut("{id:guid}")]
    public Task<InventoryDocumentDto> UpdateDraftAsync(
        Guid id,
        [FromBody] UpdateInventoryDocumentDraftDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.UpdateDraftAsync(id, input, cancellationToken);

    /// <summary>Gửi phiếu vào luồng chờ duyệt (PendingApproval). Bắt buộc với phiếu có tổng giá trị vượt ngưỡng cấu hình.</summary>
    [HttpPost("{id:guid}/submit-for-approval")]
    public Task<InventoryDocumentDto> SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.SubmitForApprovalAsync(id, cancellationToken);

    /// <summary>
    /// Duyệt phiếu — hệ thống sinh StockTransaction và cập nhật CurrentStock.
    /// Đây là bước thực sự làm thay đổi tồn kho. Với phiếu chuyển kho: bước này ghi nhận xuất nguồn + nhập kho in-transit.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public Task<InventoryDocumentDto> ApproveAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.ApproveAsync(id, cancellationToken);

    /// <summary>Nhận hàng chuyển kho tại kho đích — hoàn tất luồng transfer sau khi đã approve/ship (xuất in-transit, nhập kho đích).</summary>
    [HttpPost("{id:guid}/receive-transfer")]
    public Task<InventoryDocumentDto> ReceiveTransferAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.ReceiveTransferAsync(id, cancellationToken);

    /// <summary>Hủy phiếu Draft hoặc phiếu đang chờ duyệt. Chỉ áp dụng phiếu chưa ghi sổ tồn — không tác động CurrentStock.</summary>
    [HttpPost("{id:guid}/cancel")]
    public Task<InventoryDocumentDto> CancelAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CancelAsync(id, cancellationToken);
}
