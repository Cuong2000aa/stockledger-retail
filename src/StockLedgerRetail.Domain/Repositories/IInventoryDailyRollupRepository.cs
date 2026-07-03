using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IInventoryDailyRollupRepository
{
    Task ReplaceForDateAsync(
        DateOnly snapshotDate,
        IReadOnlyList<InventoryDailyRollup> rollups,
        CancellationToken cancellationToken = default);

    Task<List<InventoryDailyRollup>> BuildRollupsForDateAsync(
        DateOnly snapshotDate,
        CancellationToken cancellationToken = default);
}
