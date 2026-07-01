using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Analytics;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>API phân tích tồn kho — tổng hợp số liệu read-only.</summary>
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsAppService _analyticsAppService;

    public AnalyticsController(IAnalyticsAppService analyticsAppService)
    {
        _analyticsAppService = analyticsAppService;
    }

    /// <summary>Tổng quan: tổng SKU, tồn, PO đang mở, GR chờ duyệt.</summary>
    [HttpGet("summary")]
    public Task<InventorySummaryDto> GetSummaryAsync(CancellationToken cancellationToken) =>
        _analyticsAppService.GetSummaryAsync(cancellationToken);

    /// <summary>Tồn kho gom theo kho.</summary>
    [HttpGet("stock-by-warehouse")]
    public Task<List<StockByWarehouseDto>> GetStockByWarehouseAsync(CancellationToken cancellationToken) =>
        _analyticsAppService.GetStockByWarehouseAsync(cancellationToken);

    /// <summary>Tổng nhập/xuất thực tế trong khoảng thời gian (mặc định 30 ngày). Không gồm chuyển kho nội bộ.</summary>
    [HttpGet("movements")]
    public Task<MovementSummaryDto> GetMovementSummaryAsync(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken) =>
        _analyticsAppService.GetMovementSummaryAsync(fromDate, toDate, cancellationToken);

    /// <summary>SKU tồn thấp (khả dụng &lt;= threshold).</summary>
    [HttpGet("low-stock")]
    public Task<List<LowStockItemDto>> GetLowStockAsync(
        [FromQuery] decimal threshold = 10,
        CancellationToken cancellationToken = default) =>
        _analyticsAppService.GetLowStockAsync(threshold, cancellationToken);
}
