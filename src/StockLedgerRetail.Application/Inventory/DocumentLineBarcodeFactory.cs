using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Application.Inventory;

internal static class DocumentLineBarcodeFactory
{
    public static List<InventoryDocumentLineBarcode> CreateForInventoryLine(
        Guid lineId,
        IReadOnlyList<string> barcodes) =>
        barcodes.Select(barcode => new InventoryDocumentLineBarcode
        {
            Id = Guid.NewGuid(),
            InventoryDocumentLineId = lineId,
            Barcode = barcode
        }).ToList();

    public static List<PurchaseOrderLineBarcode> CreateForPurchaseOrderLine(
        Guid lineId,
        IReadOnlyList<string> barcodes) =>
        barcodes.Select(barcode => new PurchaseOrderLineBarcode
        {
            Id = Guid.NewGuid(),
            PurchaseOrderLineId = lineId,
            Barcode = barcode
        }).ToList();

    public static List<GoodsReceiptLineBarcode> CreateForGoodsReceiptLine(
        Guid lineId,
        IReadOnlyList<string> barcodes) =>
        barcodes.Select(barcode => new GoodsReceiptLineBarcode
        {
            Id = Guid.NewGuid(),
            GoodsReceiptLineId = lineId,
            Barcode = barcode
        }).ToList();
}
