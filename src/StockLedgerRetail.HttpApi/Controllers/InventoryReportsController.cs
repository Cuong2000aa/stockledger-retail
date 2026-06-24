using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Reports;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/reports")]
public class InventoryReportsController : ControllerBase
{
    private readonly IInventoryReportsAppService _inventoryReportsAppService;

    public InventoryReportsController(IInventoryReportsAppService inventoryReportsAppService)
    {
        _inventoryReportsAppService = inventoryReportsAppService;
    }

    [HttpGet("inventory-value")]
    public Task<InventoryValueReportDto> GetInventoryValueAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _inventoryReportsAppService.GetInventoryValueAsync(
            warehouseId, brandId, page, pageSize, cancellationToken);

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

    [HttpGet("cost-history")]
    public Task<PagedResultDto<ProductCostHistoryDto>> GetCostHistoryAsync(
        [FromQuery] Guid? productVariantId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _inventoryReportsAppService.GetCostHistoryAsync(productVariantId, page, pageSize, cancellationToken);

    [HttpGet("near-expiry-lots")]
    public Task<List<NearExpiryLotDto>> GetNearExpiryLotsAsync(
        [FromQuery] int daysAhead = 30,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? brandId = null,
        CancellationToken cancellationToken = default) =>
        _inventoryReportsAppService.GetNearExpiryLotsAsync(daysAhead, warehouseId, brandId, cancellationToken);

    [HttpGet("lot-stocks")]
    public Task<PagedResultDto<LotStockDto>> GetLotStocksAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _inventoryReportsAppService.GetLotStocksAsync(warehouseId, productVariantId, page, pageSize, cancellationToken);
}
