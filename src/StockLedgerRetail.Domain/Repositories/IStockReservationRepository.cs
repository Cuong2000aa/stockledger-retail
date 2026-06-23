using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface IStockReservationRepository
{
    Task<StockReservation?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StockReservation?> GetActiveByReferenceAsync(
        string sourceSystem,
        StockReservationReferenceType referenceType,
        string referenceKey,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetActiveReservedQuantityAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<(Guid ProductVariantId, Guid WarehouseId), decimal>> GetActiveReservedQuantitiesAsync(
        IReadOnlyCollection<Guid> productVariantIds,
        IReadOnlyCollection<Guid> warehouseIds,
        CancellationToken cancellationToken = default);

    Task<List<StockReservation>> GetExpiredActiveReservationsAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default);

    Task InsertAsync(StockReservation reservation, CancellationToken cancellationToken = default);

    Task UpdateAsync(StockReservation reservation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
