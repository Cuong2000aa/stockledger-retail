using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Application.Inventory;

public interface IUnitBarcodeStockService
{
    Task ApplyInboundAsync(
        Guid productVariantId,
        Guid warehouseId,
        IReadOnlyList<string> barcodes,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task ApplyOutboundAsync(
        Guid productVariantId,
        Guid warehouseId,
        IReadOnlyList<string> barcodes,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task ApplyTransferShipAsync(
        Guid productVariantId,
        Guid sourceWarehouseId,
        Guid inTransitWarehouseId,
        IReadOnlyList<string> barcodes,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task ApplyTransferReceiveAsync(
        Guid productVariantId,
        Guid inTransitWarehouseId,
        Guid destinationWarehouseId,
        IReadOnlyList<string> barcodes,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task ValidateOutboundAsync(
        Guid productVariantId,
        Guid warehouseId,
        IReadOnlyList<string> barcodes,
        CancellationToken cancellationToken = default);

    Task ValidateInboundAsync(
        Guid productVariantId,
        IReadOnlyList<string> barcodes,
        CancellationToken cancellationToken = default);
}

public class UnitBarcodeStockService : IUnitBarcodeStockService
{
    private readonly IVariantUnitBarcodeRepository _variantUnitBarcodeRepository;

    public UnitBarcodeStockService(IVariantUnitBarcodeRepository variantUnitBarcodeRepository)
    {
        _variantUnitBarcodeRepository = variantUnitBarcodeRepository;
    }

    public async Task ValidateInboundAsync(
        Guid productVariantId,
        IReadOnlyList<string> barcodes,
        CancellationToken cancellationToken = default)
    {
        if (barcodes.Count == 0)
        {
            return;
        }

        var existing = await _variantUnitBarcodeRepository.GetByBarcodesAsync(barcodes, cancellationToken);
        var blocked = existing
            .Where(x => x.Status is UnitBarcodeStatus.InStock or UnitBarcodeStatus.InTransit)
            .Select(x => x.Barcode)
            .ToList();

        if (blocked.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unit barcodes already in stock: {string.Join(", ", blocked)}.");
        }
    }

    public async Task ValidateOutboundAsync(
        Guid productVariantId,
        Guid warehouseId,
        IReadOnlyList<string> barcodes,
        CancellationToken cancellationToken = default)
    {
        if (barcodes.Count == 0)
        {
            return;
        }

        var existing = await _variantUnitBarcodeRepository.GetByBarcodesAsync(barcodes, cancellationToken);
        var byBarcode = existing.ToDictionary(x => x.Barcode, StringComparer.OrdinalIgnoreCase);

        foreach (var barcode in barcodes)
        {
            if (!byBarcode.TryGetValue(barcode, out var unit))
            {
                throw new InvalidOperationException($"Unit barcode '{barcode}' was not found.");
            }

            if (unit.ProductVariantId != productVariantId)
            {
                throw new InvalidOperationException(
                    $"Unit barcode '{barcode}' does not belong to the selected SKU.");
            }

            if (unit.WarehouseId != warehouseId || unit.Status != UnitBarcodeStatus.InStock)
            {
                throw new InvalidOperationException(
                    $"Unit barcode '{barcode}' is not in stock at the selected warehouse.");
            }
        }
    }

    public async Task ApplyInboundAsync(
        Guid productVariantId,
        Guid warehouseId,
        IReadOnlyList<string> barcodes,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (barcodes.Count == 0)
        {
            return;
        }

        await ValidateInboundAsync(productVariantId, barcodes, cancellationToken);

        var existing = await _variantUnitBarcodeRepository.GetByBarcodesAsync(barcodes, cancellationToken);
        var byBarcode = existing.ToDictionary(x => x.Barcode, StringComparer.OrdinalIgnoreCase);
        var inserts = new List<VariantUnitBarcode>();
        var updates = new List<VariantUnitBarcode>();

        foreach (var barcode in barcodes)
        {
            if (byBarcode.TryGetValue(barcode, out var unit))
            {
                unit.ProductVariantId = productVariantId;
                unit.WarehouseId = warehouseId;
                unit.Status = UnitBarcodeStatus.InStock;
                unit.LastUpdatedAt = now;
                updates.Add(unit);
            }
            else
            {
                inserts.Add(new VariantUnitBarcode
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = productVariantId,
                    Barcode = barcode,
                    WarehouseId = warehouseId,
                    Status = UnitBarcodeStatus.InStock,
                    ReceivedAt = now,
                    LastUpdatedAt = now
                });
            }
        }

        if (inserts.Count > 0)
        {
            await _variantUnitBarcodeRepository.InsertRangeAsync(inserts, cancellationToken);
        }

        if (updates.Count > 0)
        {
            await _variantUnitBarcodeRepository.UpdateRangeAsync(updates, cancellationToken);
        }
    }

    public async Task ApplyOutboundAsync(
        Guid productVariantId,
        Guid warehouseId,
        IReadOnlyList<string> barcodes,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (barcodes.Count == 0)
        {
            return;
        }

        await ValidateOutboundAsync(productVariantId, warehouseId, barcodes, cancellationToken);

        var existing = await _variantUnitBarcodeRepository.GetByBarcodesAsync(barcodes, cancellationToken);
        foreach (var unit in existing)
        {
            unit.Status = UnitBarcodeStatus.OutOfStock;
            unit.WarehouseId = null;
            unit.LastUpdatedAt = now;
        }

        await _variantUnitBarcodeRepository.UpdateRangeAsync(existing, cancellationToken);
    }

    public async Task ApplyTransferShipAsync(
        Guid productVariantId,
        Guid sourceWarehouseId,
        Guid inTransitWarehouseId,
        IReadOnlyList<string> barcodes,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (barcodes.Count == 0)
        {
            return;
        }

        await ValidateOutboundAsync(productVariantId, sourceWarehouseId, barcodes, cancellationToken);

        var existing = await _variantUnitBarcodeRepository.GetByBarcodesAsync(barcodes, cancellationToken);
        foreach (var unit in existing)
        {
            unit.WarehouseId = inTransitWarehouseId;
            unit.Status = UnitBarcodeStatus.InTransit;
            unit.LastUpdatedAt = now;
        }

        await _variantUnitBarcodeRepository.UpdateRangeAsync(existing, cancellationToken);
    }

    public async Task ApplyTransferReceiveAsync(
        Guid productVariantId,
        Guid inTransitWarehouseId,
        Guid destinationWarehouseId,
        IReadOnlyList<string> barcodes,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (barcodes.Count == 0)
        {
            return;
        }

        var existing = await _variantUnitBarcodeRepository.GetByBarcodesAsync(barcodes, cancellationToken);
        var byBarcode = existing.ToDictionary(x => x.Barcode, StringComparer.OrdinalIgnoreCase);

        foreach (var barcode in barcodes)
        {
            if (!byBarcode.TryGetValue(barcode, out var unit))
            {
                throw new InvalidOperationException($"Unit barcode '{barcode}' was not found.");
            }

            if (unit.ProductVariantId != productVariantId)
            {
                throw new InvalidOperationException(
                    $"Unit barcode '{barcode}' does not belong to the selected SKU.");
            }

            if (unit.WarehouseId != inTransitWarehouseId || unit.Status != UnitBarcodeStatus.InTransit)
            {
                throw new InvalidOperationException(
                    $"Unit barcode '{barcode}' is not in transit for this transfer.");
            }

            unit.WarehouseId = destinationWarehouseId;
            unit.Status = UnitBarcodeStatus.InStock;
            unit.LastUpdatedAt = now;
        }

        await _variantUnitBarcodeRepository.UpdateRangeAsync(existing, cancellationToken);
    }
}
