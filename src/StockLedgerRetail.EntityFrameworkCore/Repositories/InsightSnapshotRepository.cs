using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class InsightSnapshotRepository : IInsightSnapshotRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public InsightSnapshotRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<InsightSnapshot?> GetByKeyAsync(string snapshotKey, CancellationToken cancellationToken = default) =>
        _dbContext.InsightSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SnapshotKey == snapshotKey, cancellationToken);

    public async Task UpsertAsync(InsightSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.InsightSnapshots
            .FirstOrDefaultAsync(x => x.SnapshotKey == snapshot.SnapshotKey, cancellationToken);

        if (existing is null)
        {
            if (snapshot.Id == Guid.Empty)
            {
                snapshot.Id = Guid.NewGuid();
            }

            await _dbContext.InsightSnapshots.AddAsync(snapshot, cancellationToken);
        }
        else
        {
            existing.PayloadJson = snapshot.PayloadJson;
            existing.GeneratedAtUtc = snapshot.GeneratedAtUtc;
            existing.InsightKind = snapshot.InsightKind;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByInsightKindAsync(string insightKind, CancellationToken cancellationToken = default)
    {
        await _dbContext.InsightSnapshots
            .Where(x => x.InsightKind == insightKind)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
