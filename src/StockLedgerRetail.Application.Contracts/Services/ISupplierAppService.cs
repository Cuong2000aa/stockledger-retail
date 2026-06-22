using StockLedgerRetail.Common;
using StockLedgerRetail.Suppliers;

namespace StockLedgerRetail.Services;

public interface ISupplierAppService
{
    Task<PagedResultDto<SupplierDto>> GetListAsync(
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<SupplierDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SupplierDto> CreateAsync(CreateSupplierDto input, CancellationToken cancellationToken = default);

    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
