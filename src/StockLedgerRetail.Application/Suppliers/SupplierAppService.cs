using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;
using StockLedgerRetail.Suppliers;

namespace StockLedgerRetail.Application.Suppliers;

/// <summary>Dịch vụ quản lý nhà cung cấp (Supplier).</summary>
public class SupplierAppService : ISupplierAppService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ITransactionAuditService _transactionAuditService;

    public SupplierAppService(
        ISupplierRepository supplierRepository,
        ITransactionAuditService transactionAuditService)
    {
        _supplierRepository = supplierRepository;
        _transactionAuditService = transactionAuditService;
    }

    public async Task<PagedResultDto<SupplierDto>> GetListAsync(
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _supplierRepository.GetPagedListAsync(skip, take, cancellationToken);
        return PagingNormalizer.Create(items.Select(MapToDto).ToList(), totalCount, normalizedPage, normalizedPageSize);
    }

    public async Task<SupplierDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier '{id}' was not found.");
        return MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto input, CancellationToken cancellationToken = default)
    {
        var code = input.Code.Trim();
        var existing = await _supplierRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Supplier code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = input.Name.Trim(),
            ContactName = input.ContactName?.Trim(),
            Phone = input.Phone?.Trim(),
            Email = input.Email?.Trim(),
            Address = input.Address?.Trim(),
            Status = input.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _supplierRepository.InsertAsync(supplier, cancellationToken);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(supplier);
        await _transactionAuditService.LogAsync(nameof(Supplier), supplier.Id, AuditActionType.Create, null, dto, cancellationToken);
        return dto;
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto input, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier '{id}' was not found.");

        var oldDto = MapToDto(supplier);
        supplier.Name = input.Name.Trim();
        supplier.ContactName = input.ContactName?.Trim();
        supplier.Phone = input.Phone?.Trim();
        supplier.Email = input.Email?.Trim();
        supplier.Address = input.Address?.Trim();
        supplier.Status = input.Status;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier, cancellationToken);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        var newDto = MapToDto(supplier);
        await _transactionAuditService.LogAsync(nameof(Supplier), supplier.Id, AuditActionType.Update, oldDto, newDto, cancellationToken);
        return newDto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier '{id}' was not found.");

        var oldDto = MapToDto(supplier);
        await _supplierRepository.DeleteAsync(supplier, cancellationToken);
        await _supplierRepository.SaveChangesAsync(cancellationToken);
        await _transactionAuditService.LogAsync(nameof(Supplier), supplier.Id, AuditActionType.Delete, oldDto, null, cancellationToken);
    }

    private static SupplierDto MapToDto(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Code = supplier.Code,
        Name = supplier.Name,
        ContactName = supplier.ContactName,
        Phone = supplier.Phone,
        Email = supplier.Email,
        Address = supplier.Address,
        Status = supplier.Status,
        CreatedAt = supplier.CreatedAt,
        UpdatedAt = supplier.UpdatedAt
    };
}
