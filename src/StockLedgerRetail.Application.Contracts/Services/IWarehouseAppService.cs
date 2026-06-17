using StockLedgerRetail.Warehouses;

namespace StockLedgerRetail.Services;

public interface IWarehouseAppService
{
    Task<List<WarehouseDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<WarehouseDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WarehouseDto> CreateAsync(CreateWarehouseDto input, CancellationToken cancellationToken = default);

    Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
