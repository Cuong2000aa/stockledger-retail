using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class GoodsReceiptRepository : IGoodsReceiptRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public GoodsReceiptRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<GoodsReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.GoodsReceipts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<GoodsReceipt?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.GoodsReceipts
            .Include(x => x.Lines)
                .ThenInclude(l => l.ProductVariant)
            .Include(x => x.Lines)
                .ThenInclude(l => l.PurchaseOrderLine)
            .Include(x => x.Lines)
                .ThenInclude(l => l.UnitBarcodes)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(List<GoodsReceipt> Items, int TotalCount)> GetPagedListAsync(
        Guid? purchaseOrderId,
        GoodsReceiptStatus? status,
        int skip,
        int take,
        Guid? warehouseId = null,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return ([], 0);
        }

        var query = _dbContext.GoodsReceipts
            .Include(x => x.PurchaseOrder)
            .Include(x => x.Warehouse)
            .AsQueryable();

        if (purchaseOrderId.HasValue)
        {
            query = query.Where(x => x.PurchaseOrderId == purchaseOrderId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }
        else if (scopedWarehouseIds is { Count: > 0 })
        {
            query = query.Where(x => scopedWarehouseIds.Contains(x.WarehouseId));
        }

        query = query.OrderByDescending(x => x.ReceiptDate).ThenByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default) =>
        _dbContext.GoodsReceipts.CountAsync(x => x.GrNo.StartsWith(datePrefix), cancellationToken);

    public async Task InsertAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default) =>
        await _dbContext.GoodsReceipts.AddAsync(goodsReceipt, cancellationToken);

    public Task UpdateAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
    {
        _dbContext.GoodsReceipts.Update(goodsReceipt);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
