using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.StockReservations;

namespace StockLedgerRetail.Services;

public interface IStockReservationQueryAppService
{
    Task<PagedResultDto<StockReservationListItemDto>> GetListAsync(
        Guid? warehouseId = null,
        StockReservationStatus? status = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<StockReservationListItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task ReleaseAsync(Guid id, CancellationToken cancellationToken = default);
}
