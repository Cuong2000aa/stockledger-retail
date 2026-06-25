using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface IVariantUnitBarcodeRepository
{
    Task<List<VariantUnitBarcode>> GetByBarcodesAsync(
        IEnumerable<string> barcodes,
        CancellationToken cancellationToken = default);

    Task<(List<VariantUnitBarcode> Items, int TotalCount)> GetPagedListAsync(
        Guid productVariantId,
        Guid warehouseId,
        UnitBarcodeStatus? status,
        int skip,
        int take,
        string? search,
        CancellationToken cancellationToken = default);

    Task InsertRangeAsync(
        IEnumerable<VariantUnitBarcode> items,
        CancellationToken cancellationToken = default);

    Task UpdateRangeAsync(
        IEnumerable<VariantUnitBarcode> items,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
