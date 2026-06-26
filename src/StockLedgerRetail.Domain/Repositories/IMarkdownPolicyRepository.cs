using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IMarkdownPolicyRepository
{
    Task<List<MarkdownPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);

    Task<List<MarkdownPolicy>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MarkdownPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task InsertAsync(MarkdownPolicy policy, CancellationToken cancellationToken = default);

    Task UpdateAsync(MarkdownPolicy policy, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
