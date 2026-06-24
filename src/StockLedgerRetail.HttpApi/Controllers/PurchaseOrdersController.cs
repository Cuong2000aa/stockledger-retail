using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.PurchaseOrders;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>API quản lý đơn mua hàng (Purchase Order).</summary>
[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public PurchaseOrdersController(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    /// <summary>Danh sách PO có phân trang, lọc theo trạng thái và nhà cung cấp.</summary>
    [HttpGet]
    public Task<PagedResultDto<PurchaseOrderDto>> GetListAsync(
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] Guid? supplierId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        _purchaseOrderAppService.GetListAsync(status, supplierId, page, pageSize, search, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _purchaseOrderAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo PO ở trạng thái Draft — chưa tác động tồn kho.</summary>
    [HttpPost]
    public Task<PurchaseOrderDto> CreateAsync([FromBody] CreatePurchaseOrderDto input, CancellationToken cancellationToken) =>
        _purchaseOrderAppService.CreateAsync(input, cancellationToken);

    /// <summary>Gửi PO — chuyển Draft sang Submitted, sẵn sàng nhận hàng.</summary>
    [HttpPost("{id:guid}/submit")]
    public Task<PurchaseOrderDto> SubmitAsync(Guid id, CancellationToken cancellationToken) =>
        _purchaseOrderAppService.SubmitAsync(id, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public Task<PurchaseOrderDto> ApproveAsync(Guid id, CancellationToken cancellationToken) =>
        _purchaseOrderAppService.ApproveAsync(id, cancellationToken);

    /// <summary>Hủy PO — chỉ khi chưa nhận hàng.</summary>
    [HttpPost("{id:guid}/cancel")]
    public Task<PurchaseOrderDto> CancelAsync(Guid id, CancellationToken cancellationToken) =>
        _purchaseOrderAppService.CancelAsync(id, cancellationToken);
}
