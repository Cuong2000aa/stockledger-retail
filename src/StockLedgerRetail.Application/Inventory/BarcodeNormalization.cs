using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Application.Inventory;

public static class BarcodeNormalization
{
    public static List<string> Normalize(IEnumerable<string>? barcodes)
    {
        if (barcodes is null)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var barcode in barcodes)
        {
            var trimmed = barcode.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    public static List<string> FromLine(InventoryDocumentLine line) =>
        Normalize(line.UnitBarcodes.Select(x => x.Barcode));

    public static List<string> FromLine(GoodsReceiptLine line) =>
        Normalize(line.UnitBarcodes.Select(x => x.Barcode));

    public static List<string> FromLine(PurchaseOrderLine line) =>
        Normalize(line.UnitBarcodes.Select(x => x.Barcode));
}
