using Microsoft.Extensions.DependencyInjection;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.GoodsReceipts;
using StockLedgerRetail.PurchaseOrders;
using StockLedgerRetail.Services;
using Xunit;

namespace StockLedgerRetail.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class ProcurementFlowIntegrationTests(IntegrationTestFixture fixture)
{
    private void RequireDatabase()
    {
        Assert.True(
            fixture.DatabaseReady,
            fixture.DatabaseSkipReason ?? "Integration database is not available.");
    }

    [Fact]
    public async Task FullFlow_PoGrStockIn_IncreasesOnHandAndLinksDocuments()
    {
        RequireDatabase();
        await using var scope = fixture.CreateScope();
        var services = scope.ServiceProvider;

        const decimal orderQty = 10m;
        var stockBefore = await ProcurementFlowHelpers.GetOnHandAsync(
            services,
            ProcurementTestData.SauceVariantId,
            ProcurementTestData.DominosStoreId);

        var po = await ProcurementFlowHelpers.CreateSubmittedPurchaseOrderAsync(services, orderQty);
        var gr = await ProcurementFlowHelpers.CreateAndApproveGoodsReceiptAsync(
            services,
            po,
            orderQty,
            lotSuffix: "full");

        var poAfter = await services.GetRequiredService<IPurchaseOrderAppService>().GetAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.Received, poAfter.Status);
        Assert.Equal(orderQty, poAfter.Lines.Single().ReceivedQuantity);

        Assert.Equal(GoodsReceiptStatus.Approved, gr.Status);
        Assert.NotNull(gr.InventoryDocumentId);

        var stockIn = await services.GetRequiredService<IInventoryDocumentRepository>()
            .GetByIdWithLinesAsync(gr.InventoryDocumentId!.Value)
            ?? throw new InvalidOperationException("Linked stock-in was not found.");

        Assert.Equal(InventoryDocumentType.StockIn, stockIn.DocumentType);
        Assert.Equal(InventoryDocumentStatus.Approved, stockIn.Status);
        Assert.Equal("PROCUREMENT", stockIn.SourceSystem);
        Assert.Equal(gr.GrNo, stockIn.ReferenceNo);
        Assert.Equal(ProcurementTestData.DominosStoreId, stockIn.DestinationWarehouseId);
        Assert.Equal(orderQty, stockIn.Lines.Sum(l => l.Quantity));

        var stockAfter = await ProcurementFlowHelpers.GetOnHandAsync(
            services,
            ProcurementTestData.SauceVariantId,
            ProcurementTestData.DominosStoreId);
        Assert.Equal(stockBefore + orderQty, stockAfter);

        var transactions = await services.GetRequiredService<IStockTransactionRepository>()
            .GetListAsync(
                ProcurementTestData.DominosStoreId,
                ProcurementTestData.SauceVariantId);
        Assert.Contains(transactions, t =>
            t.QuantityDelta == orderQty && t.TransactionType == StockTransactionType.In);
    }

    [Fact]
    public async Task PartialReceipt_UpdatesPoStatusAndStockInTwoSteps()
    {
        RequireDatabase();
        await using var scope = fixture.CreateScope();
        var services = scope.ServiceProvider;

        const decimal orderQty = 100m;
        const decimal firstReceipt = 40m;
        const decimal secondReceipt = 60m;

        var stockBefore = await ProcurementFlowHelpers.GetOnHandAsync(
            services,
            ProcurementTestData.SauceVariantId,
            ProcurementTestData.DominosStoreId);

        var po = await ProcurementFlowHelpers.CreateSubmittedPurchaseOrderAsync(services, orderQty);

        var gr1 = await ProcurementFlowHelpers.CreateAndApproveGoodsReceiptAsync(
            services, po, firstReceipt, "partial-1");

        var poPartial = await services.GetRequiredService<IPurchaseOrderAppService>().GetAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, poPartial.Status);
        Assert.Equal(firstReceipt, poPartial.Lines.Single().ReceivedQuantity);

        var gr2 = await ProcurementFlowHelpers.CreateAndApproveGoodsReceiptAsync(
            services, poPartial, secondReceipt, "partial-2");

        var poFinal = await services.GetRequiredService<IPurchaseOrderAppService>().GetAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.Received, poFinal.Status);
        Assert.Equal(orderQty, poFinal.Lines.Single().ReceivedQuantity);

        Assert.NotEqual(gr1.Id, gr2.Id);
        Assert.NotNull(gr1.InventoryDocumentId);
        Assert.NotNull(gr2.InventoryDocumentId);

        var stockAfter = await ProcurementFlowHelpers.GetOnHandAsync(
            services,
            ProcurementTestData.SauceVariantId,
            ProcurementTestData.DominosStoreId);
        Assert.Equal(stockBefore + orderQty, stockAfter);
    }

    [Fact]
    public async Task HighValuePo_RequiresTwoStepApprovalBeforeGoodsReceipt()
    {
        RequireDatabase();
        await using var scope = fixture.CreateScope();
        var services = scope.ServiceProvider;
        var purchaseOrders = services.GetRequiredService<IPurchaseOrderAppService>();

        const decimal orderQty = 106m;
        var po = await purchaseOrders.CreateAsync(new CreatePurchaseOrderDto
        {
            SupplierId = ProcurementTestData.DominosSupplierId,
            WarehouseId = ProcurementTestData.DominosStoreId,
            ReferenceNo = $"IT-PO-HIGH-{Guid.NewGuid():N}",
            Lines =
            [
                new CreatePurchaseOrderLineDto
                {
                    ProductVariantId = ProcurementTestData.SauceVariantId,
                    OrderedQuantity = orderQty,
                    UnitCost = ProcurementTestData.SauceUnitCost
                }
            ]
        });

        po = await purchaseOrders.SubmitAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, po.Status);
        Assert.Equal(2, po.RequiredApprovalSteps);

        po = await purchaseOrders.ApproveAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, po.Status);
        Assert.Equal(1, po.CompletedApprovalSteps);

        po = await purchaseOrders.ApproveAsync(po.Id);
        Assert.Equal(PurchaseOrderStatus.Submitted, po.Status);

        var gr = await ProcurementFlowHelpers.CreateAndApproveGoodsReceiptAsync(
            services, po, 5m, "high-value");

        Assert.Equal(GoodsReceiptStatus.Approved, gr.Status);
        Assert.NotNull(gr.InventoryDocumentId);
    }

    [Fact]
    public async Task ApproveGoodsReceiptTwice_Throws()
    {
        RequireDatabase();
        await using var scope = fixture.CreateScope();
        var services = scope.ServiceProvider;

        var po = await ProcurementFlowHelpers.CreateSubmittedPurchaseOrderAsync(services, 5m);
        var gr = await ProcurementFlowHelpers.CreateAndApproveGoodsReceiptAsync(
            services, po, 5m, "idempotent");

        var goodsReceipts = services.GetRequiredService<IGoodsReceiptAppService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => goodsReceipts.ApproveAsync(gr.Id));
    }
}
