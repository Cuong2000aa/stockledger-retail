using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Services;
using StockLedgerRetail.TransferPolicies;

namespace StockLedgerRetail.Application.TransferPolicies;

public class TransferPolicyAppService : ITransferPolicyAppService
{
    private readonly ITransferPolicyRepository _transferPolicyRepository;
    private readonly IBrandRepository _brandRepository;

    public TransferPolicyAppService(
        ITransferPolicyRepository transferPolicyRepository,
        IBrandRepository brandRepository)
    {
        _transferPolicyRepository = transferPolicyRepository;
        _brandRepository = brandRepository;
    }

    public async Task<List<TransferPolicyDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var policies = await _transferPolicyRepository.GetAllAsync(cancellationToken);
        var brands = (await _brandRepository.GetListAsync(cancellationToken))
            .ToDictionary(x => x.Id);

        return policies.Select(p => MapToDto(p, brands)).ToList();
    }

    public async Task<TransferPolicyDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await _transferPolicyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer policy '{id}' was not found.");

        var brands = (await _brandRepository.GetListAsync(cancellationToken))
            .ToDictionary(x => x.Id);

        return MapToDto(policy, brands);
    }

    public async Task<TransferPolicyDto> CreateAsync(
        CreateTransferPolicyDto input,
        CancellationToken cancellationToken = default)
    {
        var policy = new TransferPolicy
        {
            Id = Guid.NewGuid(),
            SourceBrandId = input.SourceBrandId,
            DestinationBrandId = input.DestinationBrandId,
            AllowCrossBrand = input.AllowCrossBrand,
            IsActive = true,
            Note = input.Note?.Trim()
        };

        await _transferPolicyRepository.InsertAsync(policy, cancellationToken);
        await _transferPolicyRepository.SaveChangesAsync(cancellationToken);

        return await GetAsync(policy.Id, cancellationToken);
    }

    public async Task<TransferPolicyDto> UpdateAsync(
        Guid id,
        UpdateTransferPolicyDto input,
        CancellationToken cancellationToken = default)
    {
        var policy = await _transferPolicyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer policy '{id}' was not found.");

        policy.AllowCrossBrand = input.AllowCrossBrand;
        policy.IsActive = input.IsActive;
        policy.Note = input.Note?.Trim();

        await _transferPolicyRepository.UpdateAsync(policy, cancellationToken);
        await _transferPolicyRepository.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    private static TransferPolicyDto MapToDto(
        TransferPolicy policy,
        Dictionary<Guid, Brand> brands) => new()
    {
        Id = policy.Id,
        SourceBrandId = policy.SourceBrandId,
        SourceBrandName = policy.SourceBrandId.HasValue && brands.TryGetValue(policy.SourceBrandId.Value, out var sb)
            ? sb.Name
            : null,
        DestinationBrandId = policy.DestinationBrandId,
        DestinationBrandName = policy.DestinationBrandId.HasValue && brands.TryGetValue(policy.DestinationBrandId.Value, out var db)
            ? db.Name
            : null,
        AllowCrossBrand = policy.AllowCrossBrand,
        IsActive = policy.IsActive,
        Note = policy.Note
    };
}
