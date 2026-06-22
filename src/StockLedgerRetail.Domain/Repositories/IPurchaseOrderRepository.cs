using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PurchaseOrder?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(List<PurchaseOrder> Items, int TotalCount)> GetPagedListAsync(
        PurchaseOrderStatus? status,
        Guid? supplierId,
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default);

    Task InsertAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

    Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
