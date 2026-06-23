using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>
/// Quản lý giá vốn SKU — WAC khi nhập, COGS khi xuất, lịch sử ProductCostHistory.
/// </summary>
public class InventoryValuationService : IInventoryValuationService
{
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IProductCostHistoryRepository _productCostHistoryRepository;

    public InventoryValuationService(
        IProductVariantRepository productVariantRepository,
        IProductCostHistoryRepository productCostHistoryRepository)
    {
        _productVariantRepository = productVariantRepository;
        _productCostHistoryRepository = productCostHistoryRepository;
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
            await _productCostHistoryRepository.UpdateAsync(activeHistory, cancellationToken);
        }

        await _productCostHistoryRepository.InsertAsync(new ProductCostHistory
        {
            Id = Guid.NewGuid(),
            ProductVariantId = productVariantId,
            CostPrice = newCost,
            CostSource = costSource,
            EffectiveFrom = effectiveAt
        }, cancellationToken);

        variant.CostPrice = newCost;
        variant.CostSource = costSource;
        await _productVariantRepository.UpdateAsync(variant, cancellationToken);

        return newCost;
    }

    public async Task<decimal?> ResolveIssueUnitCostAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var variant = await _productVariantRepository.GetByIdAsync(productVariantId, cancellationToken);
        return variant?.CostPrice;
    }
}
