using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface ITransferPolicyRepository
{
    Task<List<TransferPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);

    Task<List<TransferPolicy>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TransferPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task InsertAsync(TransferPolicy policy, CancellationToken cancellationToken = default);

    Task UpdateAsync(TransferPolicy policy, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
