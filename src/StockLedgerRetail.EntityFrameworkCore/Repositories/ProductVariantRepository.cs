using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public ProductVariantRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.ProductVariants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<ProductVariant?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        _dbContext.ProductVariants.FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);

    public Task<ProductVariant?> GetByBrandIdAndSkuAsync(
        Guid? brandId,
        string sku,
        CancellationToken cancellationToken = default) =>
        _dbContext.ProductVariants.FirstOrDefaultAsync(
            x => x.Sku == sku && x.BrandId == brandId,
            cancellationToken);

    public async Task<Dictionary<Guid, Guid?>> GetBrandIdsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default)
    {
        if (variantIds.Count == 0)
        {
            return new Dictionary<Guid, Guid?>();
        }

        return await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x => variantIds.Contains(x.Id))
            .Select(x => new { x.Id, BrandId = x.BrandId ?? x.Product.BrandId })
            .ToDictionaryAsync(x => x.Id, x => x.BrandId, cancellationToken);
    }

    public Task<List<ProductVariant>> GetListAsync(CancellationToken cancellationToken = default) =>
        _dbContext.ProductVariants.OrderBy(x => x.Sku).ToListAsync(cancellationToken);

    public async Task<(List<ProductVariant> Items, int TotalCount)> GetPagedListAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductVariants.Include(x => x.Product).AsQueryable();
        var term = TextSearchHelper.Normalize(search);
        if (term is not null)
        {
            var pattern = TextSearchHelper.ToLikePattern(term);
            query = query.Where(x =>
                EF.Functions.ILike(x.Sku, pattern) ||
                (x.Barcode != null && EF.Functions.ILike(x.Barcode, pattern)) ||
                EF.Functions.ILike(x.Product.ProductCode, pattern) ||
                EF.Functions.ILike(x.Product.Name, pattern));
        }

        query = query.OrderBy(x => x.Sku);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<List<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
        _dbContext.ProductVariants.Where(x => x.ProductId == productId).OrderBy(x => x.Sku).ToListAsync(cancellationToken);

    public async Task InsertAsync(ProductVariant productVariant, CancellationToken cancellationToken = default) =>
        await _dbContext.ProductVariants.AddAsync(productVariant, cancellationToken);

    public Task UpdateAsync(ProductVariant productVariant, CancellationToken cancellationToken = default)
    {
        _dbContext.ProductVariants.Update(productVariant);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ProductVariant productVariant, CancellationToken cancellationToken = default)
    {
        _dbContext.ProductVariants.Remove(productVariant);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
