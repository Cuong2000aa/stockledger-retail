using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface ICurrentStockRepository
{
    Task<CurrentStock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CurrentStock?> GetByVariantAndWarehouseAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<List<CurrentStock>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default);

    Task InsertAsync(CurrentStock currentStock, CancellationToken cancellationToken = default);

    Task UpdateAsync(CurrentStock currentStock, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
