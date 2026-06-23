using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Inventory;

namespace StockLedgerRetail.Domain.Repositories;

public interface ICurrentStockRepository
{
    Task<CurrentStock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CurrentStock?> GetByVariantAndWarehouseAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task LockVariantWarehouseAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<StockOnHandChangeResult> ApplyOnHandDeltaAsync(
        Guid productVariantId,
        Guid warehouseId,
        decimal quantityDelta,
        DateTime updatedAt,
        Guid lastTransactionId,
        CancellationToken cancellationToken = default);

    Task SyncReservedQuantityAsync(
        Guid productVariantId,
        Guid warehouseId,
        decimal reservedQuantity,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);

    Task<List<CurrentStock>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default);

    Task<(List<CurrentStock> Items, int TotalCount)> GetPagedListAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<List<CurrentStock>> GetByVariantsAndWarehousesAsync(
        IReadOnlyCollection<Guid> productVariantIds,
        IReadOnlyCollection<Guid> warehouseIds,
        CancellationToken cancellationToken = default);

    Task InsertAsync(CurrentStock currentStock, CancellationToken cancellationToken = default);

    Task UpdateAsync(CurrentStock currentStock, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
