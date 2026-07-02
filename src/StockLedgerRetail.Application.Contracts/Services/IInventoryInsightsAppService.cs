using StockLedgerRetail.Common;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Services;

public interface IInventoryInsightsAppService
{
    Task<List<DeadStockInsightDto>> GetDeadStockAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int daysWithoutOutbound = 60,
        decimal minOnHand = 1,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false);

    Task<List<SalesVelocityInsightDto>> GetSalesVelocityAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 100,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false);

    Task<List<TransferSuggestionDto>> GetTransferSuggestionsAsync(
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int targetCoverDays = 14,
        int reserveCoverDays = 7,
        int maxResults = 20,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false);

    Task<List<MarkdownCandidateInsightDto>> GetMarkdownCandidatesAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int daysWithoutOutbound = 60,
        decimal minOnHand = 1,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false);

    Task<List<PromotionRiskInsightDto>> GetPromotionRiskAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false);

    Task<List<ReorderRiskInsightDto>> GetReorderRiskAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false);

    Task<List<TrendSummaryInsightDto>> GetTrendSummaryAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false);

    Task<InsightsExecutiveSummaryDto> GetExecutiveSummaryAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int daysWithoutOutbound = 60,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false);

    Task<InsightsExecutiveSummaryDto?> TryGetExecutiveSummarySnapshotAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int daysWithoutOutbound = 60,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<DeadStockInsightDto>> GetDeadStockPagedAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int daysWithoutOutbound = 60,
        decimal minOnHand = 1,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<List<BrokenSizeRunInsightDto>> GetBrokenSizeRunsAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<List<SeasonClearanceInsightDto>> GetSeasonClearanceAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        string? currentSeason = null,
        int lookbackDays = 30,
        int daysWithoutOutbound = 60,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<MarkdownWhatIfResultDto> SimulateMarkdownWhatIfAsync(
        MarkdownWhatIfRequestDto input,
        CancellationToken cancellationToken = default);

    Task<InsightExplainResponseDto> ExplainAsync(
        InsightExplainRequestDto input,
        CancellationToken cancellationToken = default);

    Task<BulkTransferFromInsightsResultDto> CreateBulkTransfersAsync(
        BulkTransferFromInsightsRequestDto input,
        CancellationToken cancellationToken = default);
}
