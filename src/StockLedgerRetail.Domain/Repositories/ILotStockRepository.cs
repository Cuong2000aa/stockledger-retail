using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface ILotStockRepository
{
    Task<LotStock?> GetByLotAndWarehouseAsync(
        Guid stockLotId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<List<LotStock>> GetFefoLotsAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<(List<LotStock> Items, int TotalCount)> GetPagedListAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task InsertAsync(LotStock lotStock, CancellationToken cancellationToken = default);

    Task UpdateAsync(LotStock lotStock, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
