using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class InventoryValuationSnapshotRepository : IInventoryValuationSnapshotRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public InventoryValuationSnapshotRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<InventoryValuationSnapshot?> GetByVariantWarehouseAndDateAsync(
        Guid productVariantId,
        Guid warehouseId,
        DateTime snapshotDate,
        CancellationToken cancellationToken = default)
    {
        var date = snapshotDate.Date;
        return _dbContext.InventoryValuationSnapshots.FirstOrDefaultAsync(
            x => x.ProductVariantId == productVariantId
                && x.WarehouseId == warehouseId
                && x.SnapshotDate == date,
            cancellationToken);
    }

    public async Task InsertAsync(InventoryValuationSnapshot snapshot, CancellationToken cancellationToken = default) =>
        await _dbContext.InventoryValuationSnapshots.AddAsync(snapshot, cancellationToken);

    public Task UpdateAsync(InventoryValuationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _dbContext.InventoryValuationSnapshots.Update(snapshot);
        return Task.CompletedTask;
    }
}
