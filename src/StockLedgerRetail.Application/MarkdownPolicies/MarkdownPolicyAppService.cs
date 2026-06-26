using System.Text.Json;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.MarkdownPolicies;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.MarkdownPolicies;

public class MarkdownPolicyAppService : IMarkdownPolicyAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMarkdownPolicyRepository _markdownPolicyRepository;
    private readonly IBrandRepository _brandRepository;

    public MarkdownPolicyAppService(
        IMarkdownPolicyRepository markdownPolicyRepository,
        IBrandRepository brandRepository)
    {
        _markdownPolicyRepository = markdownPolicyRepository;
        _brandRepository = brandRepository;
    }

    public async Task<List<MarkdownPolicyDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var policies = await _markdownPolicyRepository.GetAllAsync(cancellationToken);
        var brands = (await _brandRepository.GetListAsync(cancellationToken))
            .ToDictionary(x => x.Id);

        return policies.Select(p => MapToDto(p, brands)).ToList();
    }

    public async Task<MarkdownPolicyDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await _markdownPolicyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Markdown policy '{id}' was not found.");

        var brands = (await _brandRepository.GetListAsync(cancellationToken))
            .ToDictionary(x => x.Id);

        return MapToDto(policy, brands);
    }

    public async Task<MarkdownPolicyDto> CreateAsync(
        CreateMarkdownPolicyDto input,
        CancellationToken cancellationToken = default)
    {
        _ = await _brandRepository.GetByIdAsync(input.BrandId, cancellationToken)
            ?? throw new KeyNotFoundException($"Brand '{input.BrandId}' was not found.");

        ValidateTiers(input.Tiers);

        var policy = new MarkdownPolicy
        {
            Id = Guid.NewGuid(),
            BrandId = input.BrandId,
            RegionCode = NormalizeOptional(input.RegionCode),
            WarehouseType = input.WarehouseType,
            LookbackDays = input.LookbackDays,
            MinDaysWithoutOutbound = input.MinDaysWithoutOutbound,
            MinOnHand = input.MinOnHand,
            MinInventoryValueAtCost = input.MinInventoryValueAtCost,
            MinGrossMarginPercent = input.MinGrossMarginPercent,
            MaxMarkdownPercent = input.MaxMarkdownPercent,
            AllowBelowCost = input.AllowBelowCost,
            RequireApprovalAbovePercent = input.RequireApprovalAbovePercent,
            SlowSellThroughThreshold = input.SlowSellThroughThreshold,
            TiersJson = SerializeTiers(input.Tiers),
            IsActive = true,
            Note = input.Note?.Trim()
        };

        await _markdownPolicyRepository.InsertAsync(policy, cancellationToken);
        await _markdownPolicyRepository.SaveChangesAsync(cancellationToken);

        return await GetAsync(policy.Id, cancellationToken);
    }

    public async Task<MarkdownPolicyDto> UpdateAsync(
        Guid id,
        UpdateMarkdownPolicyDto input,
        CancellationToken cancellationToken = default)
    {
        var policy = await _markdownPolicyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Markdown policy '{id}' was not found.");

        ValidateTiers(input.Tiers);

        policy.RegionCode = NormalizeOptional(input.RegionCode);
        policy.WarehouseType = input.WarehouseType;
        policy.LookbackDays = input.LookbackDays;
        policy.MinDaysWithoutOutbound = input.MinDaysWithoutOutbound;
        policy.MinOnHand = input.MinOnHand;
        policy.MinInventoryValueAtCost = input.MinInventoryValueAtCost;
        policy.MinGrossMarginPercent = input.MinGrossMarginPercent;
        policy.MaxMarkdownPercent = input.MaxMarkdownPercent;
        policy.AllowBelowCost = input.AllowBelowCost;
        policy.RequireApprovalAbovePercent = input.RequireApprovalAbovePercent;
        policy.SlowSellThroughThreshold = input.SlowSellThroughThreshold;
        policy.TiersJson = SerializeTiers(input.Tiers);
        policy.IsActive = input.IsActive;
        policy.Note = input.Note?.Trim();

        await _markdownPolicyRepository.UpdateAsync(policy, cancellationToken);
        await _markdownPolicyRepository.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    private static void ValidateTiers(IReadOnlyList<MarkdownPolicyTierDto> tiers)
    {
        if (tiers.Count == 0)
        {
            throw new InvalidOperationException("At least one markdown tier is required.");
        }

        foreach (var tier in tiers)
        {
            if (tier.MarkdownPercent < 0 || tier.MarkdownPercent > 100)
            {
                throw new InvalidOperationException($"Tier '{tier.TierCode}' markdown percent must be between 0 and 100.");
            }
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SerializeTiers(IReadOnlyList<MarkdownPolicyTierDto> tiers) =>
        JsonSerializer.Serialize(tiers, JsonOptions);

    private static MarkdownPolicyDto MapToDto(
        MarkdownPolicy policy,
        Dictionary<Guid, Brand> brands) => new()
    {
        Id = policy.Id,
        BrandId = policy.BrandId,
        BrandName = brands.TryGetValue(policy.BrandId, out var brand) ? brand.Name : null,
        RegionCode = policy.RegionCode,
        WarehouseType = policy.WarehouseType,
        LookbackDays = policy.LookbackDays,
        MinDaysWithoutOutbound = policy.MinDaysWithoutOutbound,
        MinOnHand = policy.MinOnHand,
        MinInventoryValueAtCost = policy.MinInventoryValueAtCost,
        MinGrossMarginPercent = policy.MinGrossMarginPercent,
        MaxMarkdownPercent = policy.MaxMarkdownPercent,
        AllowBelowCost = policy.AllowBelowCost,
        RequireApprovalAbovePercent = policy.RequireApprovalAbovePercent,
        SlowSellThroughThreshold = policy.SlowSellThroughThreshold,
        Tiers = DeserializeTiers(policy.TiersJson),
        IsActive = policy.IsActive,
        Note = policy.Note
    };

    private static List<MarkdownPolicyTierDto> DeserializeTiers(string? tiersJson)
    {
        if (string.IsNullOrWhiteSpace(tiersJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<MarkdownPolicyTierDto>>(tiersJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
