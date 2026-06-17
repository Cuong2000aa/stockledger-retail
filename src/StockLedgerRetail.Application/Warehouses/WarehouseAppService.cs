using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;
using StockLedgerRetail.Warehouses;

namespace StockLedgerRetail.Application.Warehouses;

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

    public async Task<List<WarehouseDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = await _warehouseRepository.GetListAsync(cancellationToken);
        return warehouses.Select(MapToDto).ToList();
    }

    public async Task<WarehouseDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse '{id}' was not found.");

        return MapToDto(warehouse);
    }

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

        var now = DateTime.UtcNow;
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = input.Code.Trim(),
            Name = input.Name.Trim(),
            Type = input.Type,
            ParentWarehouseId = input.ParentWarehouseId,
            Status = input.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _warehouseRepository.InsertAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(warehouse);
        await _transactionAuditService.LogAsync(nameof(Warehouse), warehouse.Id, AuditActionType.Create, null, dto, cancellationToken);

        return dto;
    }

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

        warehouse.Name = input.Name.Trim();
        warehouse.Type = input.Type;
        warehouse.ParentWarehouseId = input.ParentWarehouseId;
        warehouse.Status = input.Status;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _warehouseRepository.UpdateAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        var newDto = MapToDto(warehouse);
        await _transactionAuditService.LogAsync(nameof(Warehouse), warehouse.Id, AuditActionType.Update, oldDto, newDto, cancellationToken);

        return newDto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse '{id}' was not found.");

        var oldDto = MapToDto(warehouse);

        await _warehouseRepository.DeleteAsync(warehouse, cancellationToken);
        await _warehouseRepository.SaveChangesAsync(cancellationToken);

        await _transactionAuditService.LogAsync(nameof(Warehouse), warehouse.Id, AuditActionType.Delete, oldDto, null, cancellationToken);
    }

    private static WarehouseDto MapToDto(Warehouse warehouse) => new()
    {
        Id = warehouse.Id,
        Code = warehouse.Code,
        Name = warehouse.Name,
        Type = warehouse.Type,
        ParentWarehouseId = warehouse.ParentWarehouseId,
        Status = warehouse.Status,
        CreatedAt = warehouse.CreatedAt,
        UpdatedAt = warehouse.UpdatedAt
    };
}
