using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IInventoryValuationSnapshotRepository
{
    Task<InventoryValuationSnapshot?> GetByVariantWarehouseAndDateAsync(
        Guid productVariantId,
        Guid warehouseId,
        DateTime snapshotDate,
        CancellationToken cancellationToken = default);

    Task InsertAsync(InventoryValuationSnapshot snapshot, CancellationToken cancellationToken = default);

    Task UpdateAsync(InventoryValuationSnapshot snapshot, CancellationToken cancellationToken = default);
}
