using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public BrandRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.Brands.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public Task<List<Brand>> GetListAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Brands.OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task<List<Guid>> GetActiveBrandIdsWithStockAsync(CancellationToken cancellationToken = default)
    {
        var variantBrandIds = _dbContext.CurrentStocks
            .AsNoTracking()
            .Where(cs => cs.QuantityOnHand > 0)
            .Join(
                _dbContext.ProductVariants.AsNoTracking().Where(pv => pv.BrandId != null),
                cs => cs.ProductVariantId,
                pv => pv.Id,
                (_, pv) => pv.BrandId!.Value);

        var warehouseBrandIds = _dbContext.CurrentStocks
            .AsNoTracking()
            .Where(cs => cs.QuantityOnHand > 0)
            .Join(
                _dbContext.Warehouses.AsNoTracking().Where(w => w.BrandId != null),
                cs => cs.WarehouseId,
                w => w.Id,
                (_, w) => w.BrandId!.Value);

        var candidateIds = await variantBrandIds
            .Union(warehouseBrandIds)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Brands
            .AsNoTracking()
            .Where(b => candidateIds.Contains(b.Id) && b.Status == BrandStatus.Active)
            .OrderBy(b => b.Code)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task InsertAsync(Brand brand, CancellationToken cancellationToken = default) =>
        await _dbContext.Brands.AddAsync(brand, cancellationToken);

    public Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        _dbContext.Brands.Update(brand);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
