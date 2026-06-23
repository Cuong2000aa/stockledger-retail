using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface ITransferPolicyRepository
{
    Task<List<TransferPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);

    Task InsertAsync(TransferPolicy policy, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
