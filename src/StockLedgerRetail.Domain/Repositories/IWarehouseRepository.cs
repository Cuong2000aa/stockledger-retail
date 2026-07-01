using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<Warehouse>> GetListAsync(CancellationToken cancellationToken = default);

    Task<(List<Warehouse> Items, int TotalCount)> GetPagedListAsync(
        int skip,
        int take,
        string? search = null,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default);

    Task<List<Warehouse>> GetActiveFulfillmentWarehousesAsync(
        IReadOnlyCollection<WarehouseType> types,
        IReadOnlyCollection<Guid>? warehouseIds = null,
        Guid? brandId = null,
        string? regionCode = null,
        CancellationToken cancellationToken = default);

    Task<Warehouse?> GetInTransitByBrandIdAsync(
        Guid? brandId,
        CancellationToken cancellationToken = default);

    Task InsertAsync(Warehouse warehouse, CancellationToken cancellationToken = default);

    Task UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default);

    Task DeleteAsync(Warehouse warehouse, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
