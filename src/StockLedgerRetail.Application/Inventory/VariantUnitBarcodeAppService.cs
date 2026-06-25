using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

public class VariantUnitBarcodeAppService : IVariantUnitBarcodeAppService
{
    private readonly IVariantUnitBarcodeRepository _variantUnitBarcodeRepository;

    public VariantUnitBarcodeAppService(IVariantUnitBarcodeRepository variantUnitBarcodeRepository)
    {
        _variantUnitBarcodeRepository = variantUnitBarcodeRepository;
    }

    public async Task<PagedResultDto<VariantUnitBarcodeDto>> GetListAsync(
        Guid productVariantId,
        Guid warehouseId,
        UnitBarcodeStatus? status = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _variantUnitBarcodeRepository.GetPagedListAsync(
            productVariantId,
            warehouseId,
            status,
            skip,
            take,
            search,
            cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return PagingNormalizer.Create(dtos, totalCount, normalizedPage, normalizedPageSize);
    }

    private static VariantUnitBarcodeDto MapToDto(Domain.Entities.VariantUnitBarcode item) => new()
    {
        Id = item.Id,
        ProductVariantId = item.ProductVariantId,
        Sku = item.ProductVariant.Sku,
        Barcode = item.Barcode,
        WarehouseId = item.WarehouseId,
        WarehouseCode = item.Warehouse?.Code,
        WarehouseName = item.Warehouse?.Name,
        Status = item.Status,
        ReceivedAt = item.ReceivedAt,
        LastUpdatedAt = item.LastUpdatedAt
    };
}
