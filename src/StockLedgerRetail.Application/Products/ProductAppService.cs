using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Products;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Products;

/// <summary>
/// Dịch vụ nghiệp vụ quản lý sản phẩm cha (Product) — CRUD và ghi audit log.
/// </summary>
public class ProductAppService : IProductAppService
{
    private readonly IProductRepository _productRepository;
    private readonly ITransactionAuditService _transactionAuditService;

    public ProductAppService(
        IProductRepository productRepository,
        ITransactionAuditService transactionAuditService)
    {
        _productRepository = productRepository;
        _transactionAuditService = transactionAuditService;
    }

    /// <summary>Lấy toàn bộ danh sách sản phẩm, sắp xếp theo mã.</summary>
    public async Task<PagedResultDto<ProductDto>> GetListAsync(
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (products, totalCount) = await _productRepository.GetPagedListAsync(skip, take, cancellationToken);
        var items = products.Select(MapToDto).ToList();
        return PagingNormalizer.Create(items, totalCount, normalizedPage, normalizedPageSize);
    }

    /// <summary>Lấy một sản phẩm theo Id. Ném lỗi nếu không tồn tại.</summary>
    public async Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{id}' was not found.");

        return MapToDto(product);
    }

    /// <summary>Tạo sản phẩm mới và ghi log CREATE.</summary>
    public async Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken cancellationToken = default)
    {
        var existing = await _productRepository.GetByProductCodeAsync(input.ProductCode, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Product code '{input.ProductCode}' already exists.");
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            ProductCode = input.ProductCode.Trim(),
            Name = input.Name.Trim(),
            Brand = input.Brand?.Trim(),
            Category = input.Category?.Trim(),
            Status = input.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _productRepository.InsertAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(product);
        await _transactionAuditService.LogAsync(nameof(Product), product.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Cập nhật sản phẩm và ghi log UPDATE (lưu giá trị cũ/mới).</summary>
    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{id}' was not found.");

        var oldDto = MapToDto(product);

        product.Name = input.Name.Trim();
        product.Brand = input.Brand?.Trim();
        product.Category = input.Category?.Trim();
        product.Status = input.Status;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        var newDto = MapToDto(product);
        await _transactionAuditService.LogAsync(nameof(Product), product.Id, AuditActionType.Update, oldDto, newDto, cancellationToken);

        return newDto;
    }

    /// <summary>Xóa sản phẩm và ghi log DELETE.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{id}' was not found.");

        var oldDto = MapToDto(product);

        await _productRepository.DeleteAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        await _transactionAuditService.LogAsync(nameof(Product), product.Id, AuditActionType.Delete, oldDto, null, cancellationToken);
    }

    /// <summary>Chuyển entity Product sang DTO trả về API.</summary>
    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        ProductCode = product.ProductCode,
        Name = product.Name,
        Brand = product.Brand,
        Category = product.Category,
        Status = product.Status,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
