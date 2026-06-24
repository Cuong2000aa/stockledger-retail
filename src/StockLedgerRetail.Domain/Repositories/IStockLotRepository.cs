using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IStockLotRepository
{
    Task<StockLot?> GetByVariantAndLotCodeAsync(
        Guid productVariantId,
        string lotCode,
        CancellationToken cancellationToken = default);

    Task<List<StockLot>> GetNearExpiryAsync(
        DateTime expiryBefore,
        Guid? warehouseId,
        Guid? brandId,
        int take,
        CancellationToken cancellationToken = default);

    Task<(List<StockLot> Items, int TotalCount)> GetPagedListAsync(
        Guid? productVariantId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task InsertAsync(StockLot lot, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
