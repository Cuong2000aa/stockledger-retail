using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Products;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Products;

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

    public async Task<List<ProductDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetListAsync(cancellationToken);
        return products.Select(MapToDto).ToList();
    }

    public async Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{id}' was not found.");

        return MapToDto(product);
    }

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

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{id}' was not found.");

        var oldDto = MapToDto(product);

        await _productRepository.DeleteAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        await _transactionAuditService.LogAsync(nameof(Product), product.Id, AuditActionType.Delete, oldDto, null, cancellationToken);
    }

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
