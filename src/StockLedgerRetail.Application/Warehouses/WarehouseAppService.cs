using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;
using StockLedgerRetail.Warehouses;

namespace StockLedgerRetail.Application.Warehouses;

/// <summary>
/// Dịch vụ nghiệp vụ quản lý kho — hỗ trợ kho cha/con (store, DC, backroom...).
/// </summary>
public class WarehouseAppService : IWarehouseAppService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ITransactionAuditService _transactionAuditService;

    public WarehouseAppService(
        IWarehouseRepository warehouseRepository,
        ITransactionAuditService transactionAuditService)
    {
        _warehouseRepository = warehouseRepository;
        _transactionAuditService = transactionAuditService;
    }

    /// <summary>Lấy danh sách tất cả kho.</summary>
    public async Task<PagedResultDto<WarehouseDto>> GetListAsync(
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (warehouses, totalCount) = await _warehouseRepository.GetPagedListAsync(skip, take, search, cancellationToken);
        var items = warehouses.Select(MapToDto).ToList();
        return PagingNormalizer.Create(items, totalCount, normalizedPage, normalizedPageSize);
    }

    /// <summary>Lấy chi tiết kho theo Id.</summary>
    public async Task<WarehouseDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse '{id}' was not found.");

        return MapToDto(warehouse);
    }

    /// <summary>Tạo kho mới, kiểm tra mã kho duy nhất và kho cha (nếu có) tồn tại.</summary>
    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto input, CancellationToken cancellationToken = default)
    {
        var existing = await _warehouseRepository.GetByCodeAsync(input.Code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Warehouse code '{input.Code}' already exists.");
        }

        if (input.ParentWarehouseId.HasValue)
        {
            _ = await _warehouseRepository.GetByIdAsync(input.ParentWarehouseId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Parent warehouse '{input.ParentWarehouseId}' was not found.");
        }

        WarehouseAddressValidator.EnsureValidForType(
            input.Type,
            input.AddressLine,
            input.Ward,
            input.District,
            input.Province);

        var addressLine = NormalizeOptional(input.AddressLine);
        var ward = NormalizeOptional(input.Ward);
        var district = NormalizeOptional(input.District);
        var province = NormalizeOptional(input.Province);
        var postalCode = NormalizeOptional(input.PostalCode);

        var now = DateTime.UtcNow;
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = input.Code.Trim(),
            Name = input.Name.Trim(),
            Type = input.Type,
            ParentWarehouseId = input.ParentWarehouseId,
            Status = input.Status,
            BrandId = input.BrandId,
            RegionCode = NormalizeOptional(input.RegionCode)?.ToUpperInvariant(),
            FulfillmentPriority = input.FulfillmentPriority,
            AddressLine = addressLine,
            Ward = ward,
            District = district,
            Province = province,
            PostalCode = postalCode,
            Phone = NormalizeOptional(input.Phone),
            ContactName = NormalizeOptional(input.ContactName),
            FullAddress = WarehouseAddressFormatter.BuildFullAddress(
                addressLine,
                ward,
                district,
                province,
                postalCode),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _warehouseRepository.InsertAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(warehouse);
        await _transactionAuditService.LogAsync(nameof(Warehouse), warehouse.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

    /// <summary>Cập nhật kho; không cho phép kho tự trỏ parent là chính nó.</summary>
    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto input, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse '{id}' was not found.");

        if (input.ParentWarehouseId.HasValue)
        {
            if (input.ParentWarehouseId.Value == id)
            {
                throw new InvalidOperationException("A warehouse cannot be its own parent.");
            }

            _ = await _warehouseRepository.GetByIdAsync(input.ParentWarehouseId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Parent warehouse '{input.ParentWarehouseId}' was not found.");
        }

        var oldDto = MapToDto(warehouse);

        WarehouseAddressValidator.EnsureValidForType(
            input.Type,
            input.AddressLine,
            input.Ward,
            input.District,
            input.Province);

        warehouse.Name = input.Name.Trim();
        warehouse.Type = input.Type;
        warehouse.ParentWarehouseId = input.ParentWarehouseId;
        warehouse.Status = input.Status;
        warehouse.BrandId = input.BrandId;
        warehouse.RegionCode = NormalizeOptional(input.RegionCode)?.ToUpperInvariant();
        warehouse.FulfillmentPriority = input.FulfillmentPriority;
        warehouse.AddressLine = NormalizeOptional(input.AddressLine);
        warehouse.Ward = NormalizeOptional(input.Ward);
        warehouse.District = NormalizeOptional(input.District);
        warehouse.Province = NormalizeOptional(input.Province);
        warehouse.PostalCode = NormalizeOptional(input.PostalCode);
        warehouse.Phone = NormalizeOptional(input.Phone);
        warehouse.ContactName = NormalizeOptional(input.ContactName);
        warehouse.FullAddress = WarehouseAddressFormatter.BuildFullAddress(
            warehouse.AddressLine,
            warehouse.Ward,
            warehouse.District,
            warehouse.Province,
            warehouse.PostalCode);
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        var newDto = MapToDto(warehouse);
        await _transactionAuditService.LogAsync(nameof(Warehouse), warehouse.Id, AuditActionType.Update, oldDto, newDto, cancellationToken);

        return newDto;
    }

    /// <summary>Xóa kho.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse '{id}' was not found.");

        var oldDto = MapToDto(warehouse);

        await _warehouseRepository.DeleteAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        await _transactionAuditService.LogAsync(nameof(Warehouse), warehouse.Id, AuditActionType.Delete, oldDto, null, cancellationToken);
    }

    /// <summary>Chuyển entity Warehouse sang DTO.</summary>
    private static WarehouseDto MapToDto(Warehouse warehouse) => new()
    {
        Id = warehouse.Id,
        Code = warehouse.Code,
        Name = warehouse.Name,
        Type = warehouse.Type,
        ParentWarehouseId = warehouse.ParentWarehouseId,
        Status = warehouse.Status,
        BrandId = warehouse.BrandId,
        RegionCode = warehouse.RegionCode,
        FulfillmentPriority = warehouse.FulfillmentPriority,
        AddressLine = warehouse.AddressLine,
        Ward = warehouse.Ward,
        District = warehouse.District,
        Province = warehouse.Province,
        PostalCode = warehouse.PostalCode,
        Phone = warehouse.Phone,
        ContactName = warehouse.ContactName,
        FullAddress = warehouse.FullAddress,
        CreatedAt = warehouse.CreatedAt,
        UpdatedAt = warehouse.UpdatedAt
    };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
