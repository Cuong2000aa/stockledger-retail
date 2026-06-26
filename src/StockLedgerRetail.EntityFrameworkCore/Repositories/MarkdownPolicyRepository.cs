using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class MarkdownPolicyRepository : IMarkdownPolicyRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public MarkdownPolicyRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<MarkdownPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.MarkdownPolicies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

    public Task<List<MarkdownPolicy>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _dbContext.MarkdownPolicies
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.BrandId)
            .ToListAsync(cancellationToken);

    public Task<MarkdownPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.MarkdownPolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task InsertAsync(MarkdownPolicy policy, CancellationToken cancellationToken = default) =>
        await _dbContext.MarkdownPolicies.AddAsync(policy, cancellationToken);

    public Task UpdateAsync(MarkdownPolicy policy, CancellationToken cancellationToken = default)
    {
        _dbContext.MarkdownPolicies.Update(policy);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
