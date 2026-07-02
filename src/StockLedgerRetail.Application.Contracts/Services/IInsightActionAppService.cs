using StockLedgerRetail.Common;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Services;

public interface IInsightActionAppService
{
    Task<InsightActionLogDto> RecordAsync(RecordInsightActionDto input, CancellationToken cancellationToken = default);

    Task<List<InsightActionLogDto>> GetRecentAsync(
        int limit = 50,
        string? insightKind = null,
        CancellationToken cancellationToken = default);

    Task<InsightActionStatsDto> GetStatsAsync(
        int lookbackDays = 30,
        CancellationToken cancellationToken = default);
}
