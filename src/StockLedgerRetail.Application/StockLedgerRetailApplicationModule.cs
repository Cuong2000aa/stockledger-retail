using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StockLedgerRetail.Application.Integration;
using StockLedgerRetail.Application.Inventory;
using StockLedgerRetail.Application.ProductVariants;
using StockLedgerRetail.Application.Products;
using StockLedgerRetail.Application.Warehouses;
using StockLedgerRetail.Application.Suppliers;
using StockLedgerRetail.Application.PurchaseOrders;
using StockLedgerRetail.Application.GoodsReceipts;
using StockLedgerRetail.Application.Analytics;
using StockLedgerRetail.Application.Insights;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail;

public static class StockLedgerRetailApplicationModule
{
    public static IServiceCollection AddStockLedgerRetailApplication(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddScoped<IAuditContext, DefaultAuditContext>();
        services.AddScoped<ITransactionAuditService, TransactionAuditService>();
        services.AddScoped<IProductAppService, ProductAppService>();
        services.AddScoped<IProductVariantAppService, ProductVariantAppService>();
        services.AddScoped<IWarehouseAppService, WarehouseAppService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<IInventoryValuationService, InventoryValuationService>();
        services.AddScoped<IStockReconciliationService, StockReconciliationService>();
        services.AddScoped<IInventoryDocumentAppService, InventoryDocumentAppService>();
        services.AddScoped<ICurrentStockAppService, CurrentStockAppService>();
        services.AddScoped<IStockTransactionAppService, StockTransactionAppService>();
        services.AddScoped<ISalesIntegrationService, SalesIntegrationService>();
        services.AddScoped<IStockReservationService, StockReservationService>();
        services.AddScoped<IWarehouseFulfillmentService, WarehouseFulfillmentService>();
        services.AddScoped<ISupplierAppService, SupplierAppService>();
        services.AddScoped<IPurchaseOrderAppService, PurchaseOrderAppService>();
        services.AddScoped<IGoodsReceiptAppService, GoodsReceiptAppService>();
        services.AddScoped<IAnalyticsAppService, AnalyticsAppService>();
        services.AddScoped<IInventoryInsightsAppService, InventoryInsightsAppService>();

        if (configuration is not null)
        {
            services.Configure<SalesIntegrationOptions>(
                configuration.GetSection(SalesIntegrationOptions.SectionName));
            services.Configure<StockReconciliationOptions>(
                configuration.GetSection(StockReconciliationOptions.SectionName));
        }

        return services;
    }
}
