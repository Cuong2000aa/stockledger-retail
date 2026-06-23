using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class StockReservationRepository : IStockReservationRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public StockReservationRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StockReservation?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.StockReservations
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<StockReservation?> GetActiveByReferenceAsync(
        string sourceSystem,
        StockReservationReferenceType referenceType,
        string referenceKey,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockReservations
            .Include(x => x.Lines)
            .Where(x =>
                x.SourceSystem == sourceSystem
                && x.ReferenceType == referenceType
                && x.ReferenceKey == referenceKey
                && x.WarehouseId == warehouseId
                && x.Status == StockReservationStatus.Active)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<decimal> GetActiveReservedQuantityAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return _dbContext.StockReservationLines
            .Where(x =>
                x.ProductVariantId == productVariantId
                && x.StockReservation!.WarehouseId == warehouseId
                && x.StockReservation.Status == StockReservationStatus.Active
                && x.StockReservation.ExpiresAt > now)
            .SumAsync(x => x.Quantity, cancellationToken);
    }

    public async Task<Dictionary<(Guid ProductVariantId, Guid WarehouseId), decimal>> GetActiveReservedQuantitiesAsync(
        IReadOnlyCollection<Guid> productVariantIds,
        IReadOnlyCollection<Guid> warehouseIds,
        CancellationToken cancellationToken = default)
    {
        if (productVariantIds.Count == 0 || warehouseIds.Count == 0)
        {
            return new Dictionary<(Guid, Guid), decimal>();
        }

        var now = DateTime.UtcNow;

        var rows = await _dbContext.StockReservationLines
            .Where(x =>
                productVariantIds.Contains(x.ProductVariantId)
                && warehouseIds.Contains(x.StockReservation!.WarehouseId)
                && x.StockReservation.Status == StockReservationStatus.Active
                && x.StockReservation.ExpiresAt > now)
            .GroupBy(x => new { x.ProductVariantId, x.StockReservation!.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductVariantId,
                WarehouseId = g.Key.WarehouseId,
                Quantity = g.Sum(x => x.Quantity)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => (x.ProductVariantId, x.WarehouseId),
            x => x.Quantity);
    }

    public Task<List<StockReservation>> GetExpiredActiveReservationsAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockReservations
            .Include(x => x.Lines)
            .Where(x => x.Status == StockReservationStatus.Active && x.ExpiresAt <= asOfUtc)
            .ToListAsync(cancellationToken);

    public Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default) =>
        _dbContext.StockReservations.CountAsync(
            x => x.ReservationNo.StartsWith(datePrefix),
            cancellationToken);

    public async Task InsertAsync(StockReservation reservation, CancellationToken cancellationToken = default) =>
        await _dbContext.StockReservations.AddAsync(reservation, cancellationToken);

    public Task UpdateAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        _dbContext.StockReservations.Update(reservation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
