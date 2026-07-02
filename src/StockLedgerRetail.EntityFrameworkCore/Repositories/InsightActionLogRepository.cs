using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class InsightActionLogRepository : IInsightActionLogRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public InsightActionLogRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InsertAsync(InsightActionLog log, CancellationToken cancellationToken = default) =>
        await _dbContext.InsightActionLogs.AddAsync(log, cancellationToken);

    public Task<List<InsightActionLog>> GetRecentAsync(
        int limit,
        string? insightKind = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InsightActionLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(insightKind))
        {
            query = query.Where(x => x.InsightKind == insightKind);
        }

        return query
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<InsightActionStats> GetStatsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.InsightActionLogs
            .AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
            .Select(x => x.ActionStatus)
            .ToListAsync(cancellationToken);

        var viewed = logs.Count(x => x == InsightActionStatus.Viewed);
        var accepted = logs.Count(x => x == InsightActionStatus.Accepted);
        var dismissed = logs.Count(x => x == InsightActionStatus.Dismissed);
        var executed = logs.Count(x => x == InsightActionStatus.Executed);
        var alerts = logs.Count(x => x == InsightActionStatus.AlertTriggered);
        var total = logs.Count;
        var actionable = viewed + accepted + dismissed;
        var executionRate = actionable == 0 ? 0 : Math.Round((decimal)executed / actionable * 100, 2);

        return new InsightActionStats
        {
            ViewedCount = viewed,
            AcceptedCount = accepted,
            DismissedCount = dismissed,
            ExecutedCount = executed,
            AlertTriggeredCount = alerts,
            TotalCount = total,
            ExecutionRatePercent = executionRate
        };
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
