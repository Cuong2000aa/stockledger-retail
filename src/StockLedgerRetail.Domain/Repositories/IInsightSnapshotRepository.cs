using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IInsightSnapshotRepository
{
    Task<InsightSnapshot?> GetByKeyAsync(string snapshotKey, CancellationToken cancellationToken = default);

    Task<Dictionary<string, DateTime>> GetGeneratedAtUtcByKeysAsync(
        IReadOnlyList<string> snapshotKeys,
        CancellationToken cancellationToken = default);

    Task<List<InsightSnapshot>> GetBrandExecutiveSummariesAsync(
        int lookbackDays,
        int daysWithoutOutbound,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(InsightSnapshot snapshot, CancellationToken cancellationToken = default);

    Task DeleteByInsightKindAsync(string insightKind, CancellationToken cancellationToken = default);
}
