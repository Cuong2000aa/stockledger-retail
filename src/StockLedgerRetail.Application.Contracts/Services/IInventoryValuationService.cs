using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Services;

public interface IInventoryValuationService
{
    /// <summary>
    /// Cập nhật giá vốn SKU theo WAC khi nhập hàng có đơn giá.
  /// Trả về giá vốn mới sau khi tính.
    /// </summary>
    Task<decimal?> ApplyReceiptCostAsync(
        Guid productVariantId,
        decimal receiptQuantity,
        decimal unitCost,
        CostSource costSource,
        decimal onHandBeforeReceipt,
        DateTime effectiveAt,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy giá vốn đơn vị để ghi COGS khi xuất kho.</summary>
    Task<decimal?> ResolveIssueUnitCostAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}
