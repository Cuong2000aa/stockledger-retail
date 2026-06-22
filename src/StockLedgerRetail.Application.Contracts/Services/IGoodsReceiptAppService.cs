using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.GoodsReceipts;

namespace StockLedgerRetail.Services;

public interface IGoodsReceiptAppService
{
    Task<PagedResultDto<GoodsReceiptDto>> GetListAsync(
        Guid? purchaseOrderId = null,
        GoodsReceiptStatus? status = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<GoodsReceiptDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptDto input, CancellationToken cancellationToken = default);

    Task<GoodsReceiptDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GoodsReceiptDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
