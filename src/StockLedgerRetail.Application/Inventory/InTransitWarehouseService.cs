using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

public class InTransitWarehouseService : IInTransitWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IBrandRepository _brandRepository;

    public InTransitWarehouseService(
        IWarehouseRepository warehouseRepository,
        IBrandRepository brandRepository)
    {
        _warehouseRepository = warehouseRepository;
        _brandRepository = brandRepository;
    }

    public async Task<Guid> GetOrCreateInTransitWarehouseIdAsync(
        Guid? brandId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _warehouseRepository.GetInTransitByBrandIdAsync(brandId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        string code;
        string name;

        if (brandId.HasValue)
        {
            var brand = await _brandRepository.GetByIdAsync(brandId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Brand '{brandId}' was not found.");

            code = $"IN_TRANSIT_{brand.Code}";
            name = $"In Transit - {brand.Name}";
        }
        else
        {
            code = "IN_TRANSIT_SHARED";
            name = "In Transit - Shared";
        }

        var duplicate = await _warehouseRepository.GetByCodeAsync(code, cancellationToken);
        if (duplicate is not null)
        {
            return duplicate.Id;
        }

        var now = DateTime.UtcNow;
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Type = WarehouseType.InTransit,
            BrandId = brandId,
            Status = WarehouseStatus.Active,
            FulfillmentPriority = 9999,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _warehouseRepository.InsertAsync(warehouse, cancellationToken);
        return warehouse.Id;
    }
}
