using StockLedgerRetail.Audit;
using StockLedgerRetail.Brands;
using StockLedgerRetail.Caching;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Brands;

public class BrandAppService : IBrandAppService
{
    private readonly IBrandRepository _brandRepository;
    private readonly ITransactionAuditService _transactionAuditService;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;

    public BrandAppService(
        IBrandRepository brandRepository,
        ITransactionAuditService transactionAuditService,
        ICacheService cacheService,
        Microsoft.Extensions.Options.IOptions<CacheOptions> cacheOptions)
    {
        _brandRepository = brandRepository;
        _transactionAuditService = transactionAuditService;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<List<BrandDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<List<BrandDto>>(CacheKeys.MasterBrands, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var brands = await _brandRepository.GetListAsync(cancellationToken);
        var items = brands.Select(MapToDto).ToList();
        await _cacheService.SetAsync(
            CacheKeys.MasterBrands,
            items,
            TimeSpan.FromMinutes(_cacheOptions.MasterDataTtlMinutes),
            cancellationToken);
        return items;
    }

    public async Task<BrandDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Brand '{id}' was not found.");

        return MapToDto(brand);
    }

    public async Task<BrandDto> CreateAsync(CreateBrandDto input, CancellationToken cancellationToken = default)
    {
        var code = input.Code.Trim();
        var existing = await _brandRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Brand code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = input.Name.Trim(),
            Status = BrandStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _brandRepository.InsertAsync(brand, cancellationToken);
        await _brandRepository.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(brand);
        await _transactionAuditService.LogAsync(nameof(Brand), brand.Id, AuditActionType.Create, null, dto, cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.MasterBrands, cancellationToken);
        return dto;
    }

    public async Task<BrandDto> UpdateAsync(Guid id, UpdateBrandDto input, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Brand '{id}' was not found.");

        var oldDto = MapToDto(brand);
        brand.Name = input.Name.Trim();
        brand.Status = input.Status;
        brand.UpdatedAt = DateTime.UtcNow;

        await _brandRepository.UpdateAsync(brand, cancellationToken);
        await _brandRepository.SaveChangesAsync(cancellationToken);

        var newDto = MapToDto(brand);
        await _transactionAuditService.LogAsync(nameof(Brand), brand.Id, AuditActionType.Update, oldDto, newDto, cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.MasterBrands, cancellationToken);
        return newDto;
    }

    private static BrandDto MapToDto(Brand brand) => new()
    {
        Id = brand.Id,
        Code = brand.Code,
        Name = brand.Name,
        Status = brand.Status,
        CreatedAt = brand.CreatedAt,
        UpdatedAt = brand.UpdatedAt
    };
}
