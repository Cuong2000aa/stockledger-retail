using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class TransferPolicyRepository : ITransferPolicyRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public TransferPolicyRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TransferPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.TransferPolicies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

    public Task<List<TransferPolicy>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _dbContext.TransferPolicies
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ToListAsync(cancellationToken);

    public Task<TransferPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.TransferPolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task InsertAsync(TransferPolicy policy, CancellationToken cancellationToken = default) =>
        await _dbContext.TransferPolicies.AddAsync(policy, cancellationToken);

    public Task UpdateAsync(TransferPolicy policy, CancellationToken cancellationToken = default)
    {
        _dbContext.TransferPolicies.Update(policy);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
