using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class ProductPriceRepository : IProductPriceRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public ProductPriceRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<ProductPrice>> GetByVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default) =>
        _dbContext.ProductPrices
            .Where(x => x.ProductVariantId == productVariantId)
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public Task<ProductPrice?> GetCurrentByVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default) =>
        _dbContext.ProductPrices
            .Where(x => x.ProductVariantId == productVariantId && x.IsCurrent)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ProductPrice?> GetCurrentByVariantAndTypeAsync(
        Guid productVariantId,
        PriceType priceType,
        CancellationToken cancellationToken = default) =>
        _dbContext.ProductPrices
            .Where(x => x.ProductVariantId == productVariantId && x.PriceType == priceType && x.IsCurrent)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task InsertAsync(ProductPrice price, CancellationToken cancellationToken = default) =>
        await _dbContext.ProductPrices.AddAsync(price, cancellationToken);

    public Task UpdateAsync(ProductPrice price, CancellationToken cancellationToken = default)
    {
        _dbContext.ProductPrices.Update(price);
        return Task.CompletedTask;
    }
}
