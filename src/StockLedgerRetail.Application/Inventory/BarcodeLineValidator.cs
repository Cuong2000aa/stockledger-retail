using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>Kiểm tra danh sách barcode từng đơn vị khi SKU bật <see cref="ProductVariant.IsBarcode"/>.</summary>
public static class BarcodeLineValidator
{
    public static List<string> RequireNormalizedBarcodes(
        ProductVariant variant,
        decimal quantity,
        IEnumerable<string>? barcodes)
    {
        if (!variant.IsBarcode)
        {
            return [];
        }

        var absoluteQuantity = Math.Abs(quantity);
        if (absoluteQuantity != Math.Floor(absoluteQuantity))
        {
            throw new InvalidOperationException(
                $"SKU '{variant.Sku}' with unit barcode tracking requires whole-number quantities.");
        }

        var expectedCount = (int)absoluteQuantity;
        var normalized = BarcodeNormalization.Normalize(barcodes);

        if (normalized.Count != expectedCount)
        {
            throw new InvalidOperationException(
                $"SKU '{variant.Sku}' requires {expectedCount} unique unit barcodes, but {normalized.Count} were provided.");
        }

        return normalized;
    }
}
