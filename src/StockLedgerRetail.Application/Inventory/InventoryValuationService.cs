using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;
using StockLedgerRetail.Audit;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>
/// Quản lý giá vốn SKU — WAC khi nhập, COGS khi xuất, lịch sử ProductCostHistory.
/// </summary>
public class InventoryValuationService : IInventoryValuationService
{
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IProductCostHistoryRepository _productCostHistoryRepository;
    private readonly IInventoryValuationSnapshotRepository _inventoryValuationSnapshotRepository;
    private readonly IAuditContext _auditContext;

    public InventoryValuationService(
        IProductVariantRepository productVariantRepository,
        IProductCostHistoryRepository productCostHistoryRepository,
        IInventoryValuationSnapshotRepository inventoryValuationSnapshotRepository,
        IAuditContext auditContext)
    {
        _productVariantRepository = productVariantRepository;
        _productCostHistoryRepository = productCostHistoryRepository;
        _inventoryValuationSnapshotRepository = inventoryValuationSnapshotRepository;
        _auditContext = auditContext;
    }

    public async Task<decimal?> ApplyReceiptCostAsync(
        Guid productVariantId,
        decimal receiptQuantity,
        decimal unitCost,
        CostSource costSource,
        decimal onHandBeforeReceipt,
        DateTime effectiveAt,
        CancellationToken cancellationToken = default)
    {
        if (receiptQuantity <= 0 || unitCost < 0)
        {
            return null;
        }

        var variant = await _productVariantRepository.GetByIdAsync(productVariantId, cancellationToken)
            ?? throw new InvalidOperationException($"Product variant '{productVariantId}' was not found.");

        var previousCost = variant.CostPrice;
        decimal newCost;

        if (onHandBeforeReceipt > 0 && previousCost.HasValue)
        {
            var totalValue = onHandBeforeReceipt * previousCost.Value + receiptQuantity * unitCost;
            var totalQty = onHandBeforeReceipt + receiptQuantity;
            newCost = totalQty > 0 ? totalValue / totalQty : unitCost;
        }
        else
        {
            newCost = unitCost;
        }

        var activeHistory = await _productCostHistoryRepository.GetActiveByVariantAsync(
            productVariantId,
            cancellationToken);

        if (activeHistory is not null)
        {
            activeHistory.EffectiveTo = effectiveAt;
            activeHistory.IsCurrent = false;
            await _productCostHistoryRepository.UpdateAsync(activeHistory, cancellationToken);
        }

        await _productCostHistoryRepository.InsertAsync(new ProductCostHistory
        {
            Id = Guid.NewGuid(),
            ProductVariantId = productVariantId,
            CostPrice = newCost,
            CostSource = costSource,
            ValuationMethod = ValuationMethod.WeightedAverage,
            Currency = "VND",
            ReferenceType = "InventoryReceipt",
            EffectiveFrom = effectiveAt,
            IsCurrent = true
        }, cancellationToken);

        variant.CostPrice = newCost;
        variant.CurrentCostPrice = newCost;
        variant.CostSource = costSource;
        variant.CurrentCostSource = costSource;
        variant.CurrentCostEffectiveFrom = effectiveAt;
        await _productVariantRepository.UpdateAsync(variant, cancellationToken);

        return newCost;
    }

    public async Task<decimal?> ResolveIssueUnitCostAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(productVariantId, cancellationToken);
        return variant?.CurrentCostPrice ?? variant?.CostPrice;
    }

    public async Task UpsertSnapshotAsync(
        Guid productVariantId,
        Guid warehouseId,
        decimal quantityOnHand,
        decimal quantityReserved,
        decimal quantityAvailable,
        DateTime snapshotAt,
        CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(productVariantId, cancellationToken)
            ?? throw new InvalidOperationException($"Product variant '{productVariantId}' was not found.");

        var averageCost = variant.CurrentCostPrice ?? variant.CostPrice;
        var snapshotDate = snapshotAt.Date;
        var inventoryValue = Math.Round(quantityOnHand * (averageCost ?? 0m), 4, MidpointRounding.AwayFromZero);
        var snapshot = await _inventoryValuationSnapshotRepository.GetByVariantWarehouseAndDateAsync(
            productVariantId,
            warehouseId,
            snapshotDate,
            cancellationToken);

        if (snapshot is null)
        {
            snapshot = new InventoryValuationSnapshot
            {
                Id = Guid.NewGuid(),
                ProductVariantId = productVariantId,
                WarehouseId = warehouseId,
                QuantityOnHand = quantityOnHand,
                QuantityReserved = quantityReserved,
                QuantityAvailable = quantityAvailable,
                AverageCost = averageCost,
                InventoryValue = inventoryValue,
                SnapshotDate = snapshotDate,
                ValuationMethod = ValuationMethod.WeightedAverage,
                Currency = "VND"
            };
            AuditUserStamp.OnCreate(snapshot, _auditContext, snapshotAt);
            await _inventoryValuationSnapshotRepository.InsertAsync(snapshot, cancellationToken);
            return;
        }

        snapshot.QuantityOnHand = quantityOnHand;
        snapshot.QuantityReserved = quantityReserved;
        snapshot.QuantityAvailable = quantityAvailable;
        snapshot.AverageCost = averageCost;
        snapshot.InventoryValue = inventoryValue;
        snapshot.ValuationMethod = ValuationMethod.WeightedAverage;
        AuditUserStamp.OnUpdate(snapshot, _auditContext, snapshotAt);
        await _inventoryValuationSnapshotRepository.UpdateAsync(snapshot, cancellationToken);
    }
}
