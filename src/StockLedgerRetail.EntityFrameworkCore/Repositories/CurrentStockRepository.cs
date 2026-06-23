using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Inventory;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class CurrentStockRepository : ICurrentStockRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public CurrentStockRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CurrentStock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CurrentStocks
            .Include(x => x.ProductVariant)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CurrentStock?> GetByVariantAndWarehouseAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        _dbContext.CurrentStocks.FirstOrDefaultAsync(
            x => x.ProductVariantId == productVariantId && x.WarehouseId == warehouseId,
            cancellationToken);

    public async Task LockVariantWarehouseAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT "Id"
             FROM current_stocks
             WHERE "ProductVariantId" = {productVariantId}
               AND "WarehouseId" = {warehouseId}
             FOR UPDATE
             """,
            cancellationToken);
    }

    public async Task<StockOnHandChangeResult> ApplyOnHandDeltaAsync(
        Guid productVariantId,
        Guid warehouseId,
        decimal quantityDelta,
        DateTime updatedAt,
        Guid lastTransactionId,
        CancellationToken cancellationToken = default)
    {
        await LockVariantWarehouseAsync(productVariantId, warehouseId, cancellationToken);

        var stock = await _dbContext.CurrentStocks
            .FirstOrDefaultAsync(
                x => x.ProductVariantId == productVariantId && x.WarehouseId == warehouseId,
                cancellationToken);

        if (stock is null)
        {
            if (quantityDelta < 0)
            {
                throw new InvalidOperationException(
                    $"Insufficient available stock for variant '{productVariantId}'. Available: 0, requested: {Math.Abs(quantityDelta)}.");
            }

            stock = new CurrentStock
            {
                Id = Guid.NewGuid(),
                ProductVariantId = productVariantId,
                WarehouseId = warehouseId,
                QuantityOnHand = quantityDelta,
                QuantityReserved = 0,
                QuantityAvailable = quantityDelta,
                LastTransactionId = lastTransactionId,
                LastUpdatedAt = updatedAt
            };

            await _dbContext.CurrentStocks.AddAsync(stock, cancellationToken);

            return new StockOnHandChangeResult(
                stock.Id,
                0,
                quantityDelta,
                0,
                quantityDelta);
        }

        var beforeOnHand = stock.QuantityOnHand;

        if (quantityDelta < 0)
        {
            var decreaseQuantity = Math.Abs(quantityDelta);
            if (stock.QuantityAvailable < decreaseQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient available stock for variant '{productVariantId}'. Available: {stock.QuantityAvailable}, requested: {decreaseQuantity}.");
            }
        }

        var afterOnHand = beforeOnHand + quantityDelta;
        if (afterOnHand < 0)
        {
            throw new InvalidOperationException("Inventory quantity cannot become negative.");
        }

        stock.QuantityOnHand = afterOnHand;
        stock.QuantityAvailable = afterOnHand - stock.QuantityReserved;
        stock.LastTransactionId = lastTransactionId;
        stock.LastUpdatedAt = updatedAt;

        _dbContext.CurrentStocks.Update(stock);

        return new StockOnHandChangeResult(
            stock.Id,
            beforeOnHand,
            afterOnHand,
            stock.QuantityReserved,
            stock.QuantityAvailable);
    }

    public async Task SyncReservedQuantityAsync(
        Guid productVariantId,
        Guid warehouseId,
        decimal reservedQuantity,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        await LockVariantWarehouseAsync(productVariantId, warehouseId, cancellationToken);

        var stock = await _dbContext.CurrentStocks
            .FirstOrDefaultAsync(
                x => x.ProductVariantId == productVariantId && x.WarehouseId == warehouseId,
                cancellationToken);

        if (stock is null)
        {
            if (reservedQuantity > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot reserve stock for variant '{productVariantId}' because no on-hand balance exists.");
            }

            return;
        }

        if (reservedQuantity > stock.QuantityOnHand)
        {
            throw new InvalidOperationException(
                $"Reserved quantity cannot exceed on-hand stock for variant '{productVariantId}'.");
        }

        stock.QuantityReserved = reservedQuantity;
        stock.QuantityAvailable = stock.QuantityOnHand - reservedQuantity;
        stock.LastUpdatedAt = updatedAt;

        _dbContext.CurrentStocks.Update(stock);
    }

    public Task<List<CurrentStock>> GetByVariantsAndWarehousesAsync(
        IReadOnlyCollection<Guid> productVariantIds,
        IReadOnlyCollection<Guid> warehouseIds,
        CancellationToken cancellationToken = default)
    {
        if (productVariantIds.Count == 0 || warehouseIds.Count == 0)
        {
            return Task.FromResult(new List<CurrentStock>());
        }

        return _dbContext.CurrentStocks
            .Where(x =>
                productVariantIds.Contains(x.ProductVariantId)
                && warehouseIds.Contains(x.WarehouseId))
            .ToListAsync(cancellationToken);
    }

    public Task<List<CurrentStock>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CurrentStocks
            .Include(x => x.ProductVariant)
            .Include(x => x.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (productVariantId.HasValue)
        {
            query = query.Where(x => x.ProductVariantId == productVariantId.Value);
        }

        return query.OrderBy(x => x.Warehouse.Code).ThenBy(x => x.ProductVariant.Sku).ToListAsync(cancellationToken);
    }

    public async Task<(List<CurrentStock> Items, int TotalCount)> GetPagedListAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CurrentStocks
            .Include(x => x.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(x => x.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (productVariantId.HasValue)
        {
            query = query.Where(x => x.ProductVariantId == productVariantId.Value);
        }

        var term = TextSearchHelper.Normalize(search);
        if (term is not null)
        {
            var pattern = TextSearchHelper.ToLikePattern(term);
            query = query.Where(x =>
                EF.Functions.ILike(x.ProductVariant.Sku, pattern) ||
                (x.ProductVariant.Barcode != null &&
                 EF.Functions.ILike(x.ProductVariant.Barcode, pattern)) ||
                EF.Functions.ILike(x.ProductVariant.Product.ProductCode, pattern) ||
                EF.Functions.ILike(x.ProductVariant.Product.Name, pattern) ||
                EF.Functions.ILike(x.Warehouse.Code, pattern) ||
                EF.Functions.ILike(x.Warehouse.Name, pattern));
        }

        query = query.OrderBy(x => x.Warehouse.Code).ThenBy(x => x.ProductVariant.Sku);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task InsertAsync(CurrentStock currentStock, CancellationToken cancellationToken = default) =>
        await _dbContext.CurrentStocks.AddAsync(currentStock, cancellationToken);

    public Task UpdateAsync(CurrentStock currentStock, CancellationToken cancellationToken = default)
    {
        _dbContext.CurrentStocks.Update(currentStock);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
