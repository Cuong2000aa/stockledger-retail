using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface IGoodsReceiptRepository
{
    Task<GoodsReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GoodsReceipt?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(List<GoodsReceipt> Items, int TotalCount)> GetPagedListAsync(
        Guid? purchaseOrderId,
        GoodsReceiptStatus? status,
        int skip,
        int take,
        Guid? warehouseId = null,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default);

    Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default);

    Task InsertAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default);

    Task UpdateAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
