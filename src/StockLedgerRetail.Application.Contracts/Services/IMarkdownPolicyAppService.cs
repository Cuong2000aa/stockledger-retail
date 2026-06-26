using StockLedgerRetail.MarkdownPolicies;

namespace StockLedgerRetail.Services;

public interface IMarkdownPolicyAppService
{
    Task<List<MarkdownPolicyDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<MarkdownPolicyDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MarkdownPolicyDto> CreateAsync(CreateMarkdownPolicyDto input, CancellationToken cancellationToken = default);

    Task<MarkdownPolicyDto> UpdateAsync(Guid id, UpdateMarkdownPolicyDto input, CancellationToken cancellationToken = default);
}
