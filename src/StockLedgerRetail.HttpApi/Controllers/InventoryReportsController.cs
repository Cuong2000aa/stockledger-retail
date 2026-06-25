using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Reports;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API báo cáo tồn kho chỉ đọc cho quản trị vận hành và tài chính.
/// Bao gồm định giá tồn, NXT, lịch sử giá vốn và báo cáo lô/hạn dùng.
/// </summary>
[ApiController]
[Route("api/reports")]
public class InventoryReportsController : ControllerBase
{
    private readonly IInventoryReportsAppService _inventoryReportsAppService;

    public InventoryReportsController(IInventoryReportsAppService inventoryReportsAppService)
    {
        _inventoryReportsAppService = inventoryReportsAppService;
    }

    /// <summary>Lấy báo cáo giá trị tồn theo SKU/kho, có thể lọc theo `warehouseId` và `brandId`.</summary>
    [HttpGet("inventory-value")]
    public Task<InventoryValueReportDto> GetInventoryValueAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _inventoryReportsAppService.GetInventoryValueAsync(
            warehouseId, brandId, page, pageSize, cancellationToken);

    /// <summary>Lấy báo cáo NXT (nhập - xuất - tồn) trong khoảng `fromDate` đến `toDate`.</summary>
    [HttpGet("nxt")]
    public Task<NxtReportDto> GetNxtReportAsync(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? warehouseId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _inventoryReportsAppService.GetNxtReportAsync(
            fromDate, toDate, warehouseId, page, pageSize, cancellationToken);

    /// <summary>Lấy lịch sử giá vốn của SKU theo thời gian hiệu lực.</summary>
    [HttpGet("cost-history")]
    public Task<PagedResultDto<ProductCostHistoryDto>> GetCostHistoryAsync(
        [FromQuery] Guid? productVariantId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _inventoryReportsAppService.GetCostHistoryAsync(productVariantId, page, pageSize, cancellationToken);

    /// <summary>Lấy danh sách lô sắp hết hạn trong số ngày tới (`daysAhead`).</summary>
    [HttpGet("near-expiry-lots")]
    public Task<List<NearExpiryLotDto>> GetNearExpiryLotsAsync(
        [FromQuery] int daysAhead = 30,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? brandId = null,
        CancellationToken cancellationToken = default) =>
        _inventoryReportsAppService.GetNearExpiryLotsAsync(daysAhead, warehouseId, brandId, cancellationToken);

    /// <summary>Lấy tồn kho theo lô (lot) có phân trang, lọc theo kho và SKU.</summary>
    [HttpGet("lot-stocks")]
    public Task<PagedResultDto<LotStockDto>> GetLotStocksAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _inventoryReportsAppService.GetLotStocksAsync(warehouseId, productVariantId, page, pageSize, cancellationToken);
}
