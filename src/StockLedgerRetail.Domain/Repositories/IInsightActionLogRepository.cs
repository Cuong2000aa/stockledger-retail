using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface IInsightActionLogRepository
{
    Task InsertAsync(InsightActionLog log, CancellationToken cancellationToken = default);

    Task<List<InsightActionLog>> GetRecentAsync(
        int limit,
        string? insightKind = null,
        CancellationToken cancellationToken = default);

    Task<InsightActionStats> GetStatsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class InsightActionStats
{
    public int ViewedCount { get; set; }

    public int AcceptedCount { get; set; }

    public int DismissedCount { get; set; }

    public int ExecutedCount { get; set; }

    public int AlertTriggeredCount { get; set; }

    public int TotalCount { get; set; }

    public decimal ExecutionRatePercent { get; set; }
}
