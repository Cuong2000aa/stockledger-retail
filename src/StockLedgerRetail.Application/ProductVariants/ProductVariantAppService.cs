using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.ProductVariants;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.ProductVariants;

/// <summary>
/// Dịch vụ nghiệp vụ quản lý SKU (ProductVariant) — đơn vị tồn kho thực tế.
/// </summary>
public class ProductVariantAppService : IProductVariantAppService
{
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IProductRepository _productRepository;
    private readonly ITransactionAuditService _transactionAuditService;

    public ProductVariantAppService(
        IProductVariantRepository productVariantRepository,
        IProductRepository productRepository,
        ITransactionAuditService transactionAuditService)
    {
        _productVariantRepository = productVariantRepository;
        _productRepository = productRepository;
        _transactionAuditService = transactionAuditService;
    }

    /// <summary>Lấy toàn bộ danh sách SKU.</summary>
    public async Task<PagedResultDto<ProductVariantDto>> GetListAsync(
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (variants, totalCount) = await _productVariantRepository.GetPagedListAsync(skip, take, cancellationToken);
        var items = variants.Select(MapToDto).ToList();
        return PagingNormalizer.Create(items, totalCount, normalizedPage, normalizedPageSize);
    }

    /// <summary>Lấy các SKU thuộc một sản phẩm cha.</summary>
    public async Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var variants = await _productVariantRepository.GetByProductIdAsync(productId, cancellationToken);
        return variants.Select(MapToDto).ToList();
    }

    /// <summary>Lấy chi tiết SKU theo Id.</summary>
    public async Task<ProductVariantDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product variant '{id}' was not found.");

        return MapToDto(variant);
    }

    /// <summary>Tạo SKU mới, kiểm tra product cha tồn tại và SKU không trùng.</summary>
    public async Task<ProductVariantDto> CreateAsync(CreateProductVariantDto input, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(input.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{input.ProductId}' was not found.");

        var existingSku = await _productVariantRepository.GetBySkuAsync(input.Sku, cancellationToken);
        if (existingSku is not null)
        {
            throw new InvalidOperationException($"SKU '{input.Sku}' already exists.");
        }

        var now = DateTime.UtcNow;
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = input.Sku.Trim(),
            Barcode = input.Barcode?.Trim(),
            Color = input.Color?.Trim(),
            Size = input.Size?.Trim(),
            Season = input.Season?.Trim(),
            Unit = input.Unit?.Trim(),
            Status = input.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _productVariantRepository.InsertAsync(variant, cancellationToken);
        await _productVariantRepository.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(variant);
        await _transactionAuditService.LogAsync(nameof(ProductVariant), variant.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Cập nhật thuộc tính SKU (không đổi mã SKU).</summary>
    public async Task<ProductVariantDto> UpdateAsync(Guid id, UpdateProductVariantDto input, CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product variant '{id}' was not found.");

        var oldDto = MapToDto(variant);

        variant.Barcode = input.Barcode?.Trim();
        variant.Color = input.Color?.Trim();
        variant.Size = input.Size?.Trim();
        variant.Season = input.Season?.Trim();
        variant.Unit = input.Unit?.Trim();
        variant.Status = input.Status;
        variant.UpdatedAt = DateTime.UtcNow;

        await _productVariantRepository.UpdateAsync(variant, cancellationToken);
        await _productVariantRepository.SaveChangesAsync(cancellationToken);

        var newDto = MapToDto(variant);
        await _transactionAuditService.LogAsync(nameof(ProductVariant), variant.Id, AuditActionType.Update, oldDto, newDto, cancellationToken);

        return newDto;
    }

    /// <summary>Xóa SKU.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product variant '{id}' was not found.");

        var oldDto = MapToDto(variant);

        await _productVariantRepository.DeleteAsync(variant, cancellationToken);
        await _productVariantRepository.SaveChangesAsync(cancellationToken);

        await _transactionAuditService.LogAsync(nameof(ProductVariant), variant.Id, AuditActionType.Delete, oldDto, null, cancellationToken);
    }

    /// <summary>Chuyển entity ProductVariant sang DTO.</summary>
    private static ProductVariantDto MapToDto(ProductVariant variant) => new()
    {
        Id = variant.Id,
        ProductId = variant.ProductId,
        Sku = variant.Sku,
        Barcode = variant.Barcode,
        Color = variant.Color,
        Size = variant.Size,
        Season = variant.Season,
        Unit = variant.Unit,
        Status = variant.Status,
        CreatedAt = variant.CreatedAt,
        UpdatedAt = variant.UpdatedAt
    };
}
