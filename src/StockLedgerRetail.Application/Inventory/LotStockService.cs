using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.Application.Inventory;

public class LotStockService : ILotStockService
{
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IStockLotRepository _stockLotRepository;
    private readonly ILotStockRepository _lotStockRepository;

    public LotStockService(
        IProductVariantRepository productVariantRepository,
        IStockLotRepository stockLotRepository,
        ILotStockRepository lotStockRepository)
    {
        _productVariantRepository = productVariantRepository;
        _stockLotRepository = stockLotRepository;
        _lotStockRepository = lotStockRepository;
    }

    public async Task ApplyStockInLotAsync(
        InventoryDocumentLine line,
        Guid warehouseId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken)
            ?? throw new InvalidOperationException($"Product variant '{line.ProductVariantId}' was not found.");

        if (!variant.TrackLotExpiry)
        {
            return;
        }

        var lotCode = line.LotCode?.Trim();
        if (string.IsNullOrEmpty(lotCode))
        {
            lotCode = OpeningLotCode;
        }

        var lot = await _stockLotRepository.GetByVariantAndLotCodeAsync(
            line.ProductVariantId,
            lotCode,
            cancellationToken);

        if (lot is null)
        {
            lot = new StockLot
            {
                Id = Guid.NewGuid(),
                ProductVariantId = line.ProductVariantId,
                LotCode = lotCode,
                ExpiryDate = line.ExpiryDate,
                ReceivedAt = now
            };
            await _stockLotRepository.InsertAsync(lot, cancellationToken);
        }

        line.StockLotId = lot.Id;

        var lotStock = await _lotStockRepository.GetByLotAndWarehouseAsync(lot.Id, warehouseId, cancellationToken);
        if (lotStock is null)
        {
            lotStock = new LotStock
            {
                Id = Guid.NewGuid(),
                StockLotId = lot.Id,
                WarehouseId = warehouseId,
                QuantityOnHand = line.Quantity,
                LastUpdatedAt = now
            };
            await _lotStockRepository.InsertAsync(lotStock, cancellationToken);
        }
        else
        {
            lotStock.QuantityOnHand += line.Quantity;
            lotStock.LastUpdatedAt = now;
            await _lotStockRepository.UpdateAsync(lotStock, cancellationToken);
        }
    }

    public async Task ApplyStockOutFefoAsync(
        InventoryDocumentLine line,
        Guid warehouseId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken)
            ?? throw new InvalidOperationException($"Product variant '{line.ProductVariantId}' was not found.");

        if (!variant.TrackLotExpiry)
        {
            return;
        }

        var remaining = line.Quantity;

        if (line.StockLotId.HasValue)
        {
            var lotStock = await _lotStockRepository.GetByLotAndWarehouseAsync(
                line.StockLotId.Value,
                warehouseId,
                cancellationToken)
                ?? throw new InvalidOperationException("Specified lot stock was not found.");

            if (lotStock.QuantityOnHand < remaining)
            {
                throw new InvalidOperationException(
                    $"Insufficient lot quantity for SKU '{variant.Sku}'.");
            }

            lotStock.QuantityOnHand -= remaining;
            lotStock.LastUpdatedAt = now;
            await _lotStockRepository.UpdateAsync(lotStock, cancellationToken);
            return;
        }

        var fefoLots = await _lotStockRepository.GetFefoLotsAsync(
            line.ProductVariantId,
            warehouseId,
            cancellationToken);

        foreach (var lotStock in fefoLots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var deduct = Math.Min(remaining, lotStock.QuantityOnHand);
            lotStock.QuantityOnHand -= deduct;
            lotStock.LastUpdatedAt = now;
            await _lotStockRepository.UpdateAsync(lotStock, cancellationToken);
            remaining -= deduct;
        }

        if (remaining > 0)
        {
            await MaterializeOpeningLotAndDeductAsync(
                line.ProductVariantId,
                warehouseId,
                remaining,
                now,
                cancellationToken);
        }
    }

    private const string OpeningLotCode = "OPENING";

    /// <summary>
    /// Assigns unallocated on-hand stock to an OPENING lot, then deducts (legacy seed / imports without lots).
    /// </summary>
    private async Task MaterializeOpeningLotAndDeductAsync(
        Guid productVariantId,
        Guid warehouseId,
        decimal quantity,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var lot = await _stockLotRepository.GetByVariantAndLotCodeAsync(
            productVariantId,
            OpeningLotCode,
            cancellationToken);

        if (lot is null)
        {
            lot = new StockLot
            {
                Id = Guid.NewGuid(),
                ProductVariantId = productVariantId,
                LotCode = OpeningLotCode,
                ReceivedAt = now
            };
            await _stockLotRepository.InsertAsync(lot, cancellationToken);
        }

        var lotStock = await _lotStockRepository.GetByLotAndWarehouseAsync(lot.Id, warehouseId, cancellationToken);
        if (lotStock is null)
        {
            lotStock = new LotStock
            {
                Id = Guid.NewGuid(),
                StockLotId = lot.Id,
                WarehouseId = warehouseId,
                QuantityOnHand = quantity,
                LastUpdatedAt = now
            };
            await _lotStockRepository.InsertAsync(lotStock, cancellationToken);
        }
        else
        {
            lotStock.QuantityOnHand += quantity;
            lotStock.LastUpdatedAt = now;
            await _lotStockRepository.UpdateAsync(lotStock, cancellationToken);
        }

        if (lotStock.QuantityOnHand < quantity)
        {
            var variant = await _productVariantRepository.GetByIdAsync(productVariantId, cancellationToken);
            throw new InvalidOperationException(
                $"Insufficient lot-tracked stock for SKU '{variant?.Sku ?? productVariantId.ToString()}'.");
        }

        lotStock.QuantityOnHand -= quantity;
        lotStock.LastUpdatedAt = now;
        await _lotStockRepository.UpdateAsync(lotStock, cancellationToken);
    }
}
