using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// Read-only inventory insights for operational decision support and future AI orchestration.
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
