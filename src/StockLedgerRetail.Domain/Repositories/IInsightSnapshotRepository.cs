using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IInsightSnapshotRepository
{
    Task<InsightSnapshot?> GetByKeyAsync(string snapshotKey, CancellationToken cancellationToken = default);

    Task UpsertAsync(InsightSnapshot snapshot, CancellationToken cancellationToken = default);

    Task DeleteByInsightKindAsync(string insightKind, CancellationToken cancellationToken = default);
}
