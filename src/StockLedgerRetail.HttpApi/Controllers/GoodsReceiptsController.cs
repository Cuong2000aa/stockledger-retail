using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.GoodsReceipts;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>API nhận hàng (Goods Receipt) — duyệt sẽ nhập kho theo PO.</summary>
[ApiController]
[Route("api/goods-receipts")]
public class GoodsReceiptsController : ControllerBase
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public GoodsReceiptsController(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<GoodsReceiptDto>> GetListAsync(
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] GoodsReceiptStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _goodsReceiptAppService.GetListAsync(purchaseOrderId, status, page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<GoodsReceiptDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _goodsReceiptAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo phiếu nhận hàng Draft từ PO đã submit.</summary>
    [HttpPost]
    public Task<GoodsReceiptDto> CreateAsync([FromBody] CreateGoodsReceiptDto input, CancellationToken cancellationToken) =>
        _goodsReceiptAppService.CreateAsync(input, cancellationToken);

    /// <summary>Duyệt GR — sinh phiếu nhập kho và cập nhật số đã nhận trên PO.</summary>
    [HttpPost("{id:guid}/approve")]
    public Task<GoodsReceiptDto> ApproveAsync(Guid id, CancellationToken cancellationToken) =>
        _goodsReceiptAppService.ApproveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    public Task<GoodsReceiptDto> CancelAsync(Guid id, CancellationToken cancellationToken) =>
        _goodsReceiptAppService.CancelAsync(id, cancellationToken);
}
