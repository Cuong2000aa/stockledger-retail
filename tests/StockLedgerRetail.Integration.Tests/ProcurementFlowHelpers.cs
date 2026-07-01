using Microsoft.Extensions.DependencyInjection;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.GoodsReceipts;
using StockLedgerRetail.PurchaseOrders;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Integration.Tests;

internal static class ProcurementFlowHelpers
{
    public static async Task<decimal> GetOnHandAsync(
        IServiceProvider services,
        Guid variantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var stock = await services.GetRequiredService<ICurrentStockRepository>()
            .GetByVariantAndWarehouseAsync(variantId, warehouseId, cancellationToken);

        return stock?.QuantityOnHand ?? 0m;
    }

    public static async Task<PurchaseOrderDto> CreateSubmittedPurchaseOrderAsync(
        IServiceProvider services,
        decimal orderedQuantity,
        decimal? unitCost = null,
        CancellationToken cancellationToken = default)
    {
        var purchaseOrders = services.GetRequiredService<IPurchaseOrderAppService>();
        var cost = unitCost ?? ProcurementTestData.SauceUnitCost;

        var po = await purchaseOrders.CreateAsync(new CreatePurchaseOrderDto
        {
            SupplierId = ProcurementTestData.DominosSupplierId,
            WarehouseId = ProcurementTestData.DominosStoreId,
            ReferenceNo = $"IT-PO-{Guid.NewGuid():N}",
            Lines =
            [
                new CreatePurchaseOrderLineDto
                {
                    ProductVariantId = ProcurementTestData.SauceVariantId,
                    OrderedQuantity = orderedQuantity,
                    UnitCost = cost
                }
            ]
        }, cancellationToken);

        po = await purchaseOrders.SubmitAsync(po.Id, cancellationToken);
        if (po.Status is PurchaseOrderStatus.PendingApproval)
        {
            po = await purchaseOrders.ApproveAsync(po.Id, cancellationToken);
            if (po.Status is PurchaseOrderStatus.PendingApproval)
            {
                po = await purchaseOrders.ApproveAsync(po.Id, cancellationToken);
            }
        }

        if (po.Status is not PurchaseOrderStatus.Submitted)
        {
            throw new InvalidOperationException($"Expected submitted PO, got {po.Status}.");
        }

        return po;
    }

    public static async Task<GoodsReceiptDto> CreateAndApproveGoodsReceiptAsync(
        IServiceProvider services,
        PurchaseOrderDto po,
        decimal receivedQuantity,
        string lotSuffix,
        CancellationToken cancellationToken = default)
    {
        var poLine = po.Lines.Single();
        var goodsReceipts = services.GetRequiredService<IGoodsReceiptAppService>();

        var gr = await goodsReceipts.CreateAsync(new CreateGoodsReceiptDto
        {
            PurchaseOrderId = po.Id,
            ReferenceNo = $"IT-GR-{Guid.NewGuid():N}",
            Lines =
            [
                new CreateGoodsReceiptLineDto
                {
                    PurchaseOrderLineId = poLine.Id,
                    ReceivedQuantity = receivedQuantity,
                    LotCode = $"IT-LOT-{lotSuffix}",
                    ExpiryDate = DateTime.UtcNow.Date.AddDays(30)
                }
            ]
        }, cancellationToken);

        return await goodsReceipts.ApproveAsync(gr.Id, cancellationToken);
    }
}
