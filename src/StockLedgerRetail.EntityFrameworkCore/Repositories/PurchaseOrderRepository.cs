using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public PurchaseOrderRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PurchaseOrder?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.PurchaseOrders
            .Include(x => x.Lines)
                .ThenInclude(l => l.ProductVariant)
            .Include(x => x.Lines)
                .ThenInclude(l => l.UnitBarcodes)
            .Include(x => x.Supplier)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(List<PurchaseOrder> Items, int TotalCount)> GetPagedListAsync(
        PurchaseOrderStatus? status,
        Guid? supplierId,
        int skip,
        int take,
        string? search = null,
        Guid? warehouseId = null,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return ([], 0);
        }

        var query = _dbContext.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Warehouse)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == supplierId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }
        else if (scopedWarehouseIds is { Count: > 0 })
        {
            query = query.Where(x => scopedWarehouseIds.Contains(x.WarehouseId));
        }

        var term = TextSearchHelper.Normalize(search);
        if (term is not null)
        {
            var pattern = TextSearchHelper.ToLikePattern(term);
            query = query.Where(x =>
                EF.Functions.ILike(x.PoNo, pattern) ||
                (x.ReferenceNo != null && EF.Functions.ILike(x.ReferenceNo, pattern)) ||
                (x.Supplier != null && (
                    EF.Functions.ILike(x.Supplier.Code, pattern) ||
                    EF.Functions.ILike(x.Supplier.Name, pattern))));
        }

        query = query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default) =>
        _dbContext.PurchaseOrders.CountAsync(x => x.PoNo.StartsWith(datePrefix), cancellationToken);

    public async Task InsertAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) =>
        await _dbContext.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);

    public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        _dbContext.PurchaseOrders.Update(purchaseOrder);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
