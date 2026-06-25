using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API insight tồn kho chỉ đọc cho quản lý vận hành.
/// Tổng hợp tín hiệu tồn, giá bán, giá vốn, PO/GR và xu hướng để hỗ trợ ra quyết định.
/// </summary>
[ApiController]
[Route("api/inventory-insights")]
public class InventoryInsightsController : ControllerBase
{
    private readonly IInventoryInsightsAppService _inventoryInsightsAppService;

    public InventoryInsightsController(IInventoryInsightsAppService inventoryInsightsAppService)
    {
        _inventoryInsightsAppService = inventoryInsightsAppService;
    }

    /// <summary>
    /// SKU tồn lâu không có outbound trong số ngày cấu hình.
    /// </summary>
    [HttpGet("dead-stock")]
    public Task<List<DeadStockInsightDto>> GetDeadStockAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] string? regionCode,
        [FromQuery] int daysWithoutOutbound = 60,
        [FromQuery] decimal minOnHand = 1,
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default) =>
        _inventoryInsightsAppService.GetDeadStockAsync(
            warehouseId,
            brandId,
            regionCode,
            daysWithoutOutbound,
            minOnHand,
            maxResults,
            cancellationToken);

    /// <summary>
    /// Tốc độ outbound theo SKU và kho trong cửa sổ thời gian gần đây.
    /// </summary>
    [HttpGet("sales-velocity")]
    public Task<List<SalesVelocityInsightDto>> GetSalesVelocityAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] string? regionCode,
        [FromQuery] int lookbackDays = 30,
        [FromQuery] int maxResults = 100,
        CancellationToken cancellationToken = default) =>
        _inventoryInsightsAppService.GetSalesVelocityAsync(
            warehouseId,
            brandId,
            regionCode,
            lookbackDays,
            maxResults,
            cancellationToken);

    /// <summary>
    /// Gợi ý chuyển hàng giữa kho thừa và kho thiếu dựa trên tồn khả dụng và outbound gần đây.
    /// </summary>
    [HttpGet("transfer-suggestions")]
    public Task<List<TransferSuggestionDto>> GetTransferSuggestionsAsync(
        [FromQuery] Guid? sourceWarehouseId,
        [FromQuery] Guid? destinationWarehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] string? regionCode,
        [FromQuery] int lookbackDays = 30,
        [FromQuery] int targetCoverDays = 14,
        [FromQuery] int reserveCoverDays = 7,
        [FromQuery] int maxResults = 20,
        CancellationToken cancellationToken = default) =>
        _inventoryInsightsAppService.GetTransferSuggestionsAsync(
            sourceWarehouseId,
            destinationWarehouseId,
            brandId,
            regionCode,
            lookbackDays,
            targetCoverDays,
            reserveCoverDays,
            maxResults,
            cancellationToken);

    /// <summary>Gợi ý SKU nên markdown/xả hàng do bán chậm, kèm giá trị tồn và biên lợi nhuận.</summary>
    [HttpGet("markdown-candidates")]
    public Task<List<MarkdownCandidateInsightDto>> GetMarkdownCandidatesAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] string? regionCode,
        [FromQuery] int daysWithoutOutbound = 60,
        [FromQuery] decimal minOnHand = 1,
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default) =>
        _inventoryInsightsAppService.GetMarkdownCandidatesAsync(
            warehouseId,
            brandId,
            regionCode,
            daysWithoutOutbound,
            minOnHand,
            maxResults,
            cancellationToken);

    /// <summary>Phát hiện SKU có rủi ro khuyến mãi dựa trên giá promotion/markdown, tốc độ bán và số ngày cover.</summary>
    [HttpGet("promotion-risk")]
    public Task<List<PromotionRiskInsightDto>> GetPromotionRiskAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] string? regionCode,
        [FromQuery] int lookbackDays = 30,
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default) =>
        _inventoryInsightsAppService.GetPromotionRiskAsync(
            warehouseId,
            brandId,
            regionCode,
            lookbackDays,
            maxResults,
            cancellationToken);

    /// <summary>Phát hiện SKU có rủi ro cần đặt hàng lại dựa trên cover thấp và pipeline PO/GR đang mở.</summary>
    [HttpGet("reorder-risk")]
    public Task<List<ReorderRiskInsightDto>> GetReorderRiskAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] string? regionCode,
        [FromQuery] int lookbackDays = 30,
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default) =>
        _inventoryInsightsAppService.GetReorderRiskAsync(
            warehouseId,
            brandId,
            regionCode,
            lookbackDays,
            maxResults,
            cancellationToken);

    /// <summary>Tóm tắt xu hướng tồn kho và luân chuyển giữa kỳ hiện tại với kỳ trước cùng độ dài.</summary>
    [HttpGet("trend-summary")]
    public Task<List<TrendSummaryInsightDto>> GetTrendSummaryAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] string? regionCode,
        [FromQuery] int lookbackDays = 30,
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default) =>
        _inventoryInsightsAppService.GetTrendSummaryAsync(
            warehouseId,
            brandId,
            regionCode,
            lookbackDays,
            maxResults,
            cancellationToken);

    /// <summary>Lấy executive summary tổng hợp KPI insight theo phạm vi lọc hiện tại.</summary>
    [HttpGet("executive-summary")]
    public Task<InsightsExecutiveSummaryDto> GetExecutiveSummaryAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? brandId,
        [FromQuery] string? regionCode,
        [FromQuery] int lookbackDays = 30,
        [FromQuery] int daysWithoutOutbound = 60,
        CancellationToken cancellationToken = default) =>
        _inventoryInsightsAppService.GetExecutiveSummaryAsync(
            warehouseId,
            brandId,
            regionCode,
            lookbackDays,
            daysWithoutOutbound,
            cancellationToken);
}
