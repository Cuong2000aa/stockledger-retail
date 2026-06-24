using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.PurchaseOrders;

namespace StockLedgerRetail.Services;

public interface IPurchaseOrderAppService
{
    Task<PagedResultDto<PurchaseOrderDto>> GetListAsync(
        PurchaseOrderStatus? status = null,
        Guid? supplierId = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto input, CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
