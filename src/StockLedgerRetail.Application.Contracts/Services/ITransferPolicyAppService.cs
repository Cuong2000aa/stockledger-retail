using StockLedgerRetail.TransferPolicies;

namespace StockLedgerRetail.Services;

public interface ITransferPolicyAppService
{
    Task<List<TransferPolicyDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<TransferPolicyDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TransferPolicyDto> CreateAsync(CreateTransferPolicyDto input, CancellationToken cancellationToken = default);

    Task<TransferPolicyDto> UpdateAsync(Guid id, UpdateTransferPolicyDto input, CancellationToken cancellationToken = default);
}
