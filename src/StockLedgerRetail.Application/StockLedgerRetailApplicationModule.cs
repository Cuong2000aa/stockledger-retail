using StockLedgerRetail.Application.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StockLedgerRetail.Application.Authorization;
using StockLedgerRetail.Application.Brands;
using StockLedgerRetail.Application.Identity;
using StockLedgerRetail.Application.Integration;
using StockLedgerRetail.Application.Inventory;
using StockLedgerRetail.Application.ProductVariants;
using StockLedgerRetail.Application.Products;
using StockLedgerRetail.Application.Warehouses;
using StockLedgerRetail.Application.Suppliers;
using StockLedgerRetail.Application.PurchaseOrders;
using StockLedgerRetail.Application.GoodsReceipts;
using StockLedgerRetail.Application.Analytics;
using StockLedgerRetail.Application.Audit;
using StockLedgerRetail.Application.Insights;
using StockLedgerRetail.Application.Operations;
using StockLedgerRetail.Application.Reports;
using StockLedgerRetail.Application.Seed;
using StockLedgerRetail.Application.StockReservations;
using StockLedgerRetail.Application.MarkdownPolicies;
using StockLedgerRetail.Application.TransferPolicies;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Identity;
using StockLedgerRetail.Services;

namespace StockLedgerRetail;

public static class StockLedgerRetailApplicationModule
{
    public static IServiceCollection AddStockLedgerRetailApplication(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddScoped<IAuditContext, DefaultAuditContext>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IBrandScopeContext, BrandScopeContext>();
        services.AddScoped<IUserWarehouseScopeContext, UserWarehouseScopeContext>();
        services.AddScoped<IWarehouseScopeService, WarehouseScopeService>();
        services.AddScoped<IPermissionAuthorizationService, PermissionAuthorizationService>();
        services.AddScoped<ITransactionAuditService, TransactionAuditService>();
        services.AddScoped<ITransactionLogAppService, TransactionLogAppService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IAppUserAppService, AppUserAppService>();
        services.AddScoped<IPermissionAdminAppService, PermissionAdminAppService>();
        services.AddScoped<ITeamAppService, TeamAppService>();
        services.AddScoped<IBrandAppService, BrandAppService>();
        services.AddScoped<IProductAppService, ProductAppService>();
        services.AddScoped<IProductVariantAppService, ProductVariantAppService>();
        services.AddScoped<IWarehouseAppService, WarehouseAppService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<ITransferPolicyService, TransferPolicyService>();
        services.AddScoped<ITransferPolicyAppService, TransferPolicyAppService>();
        services.AddScoped<IMarkdownPolicyAppService, MarkdownPolicyAppService>();
        services.AddScoped<IMarkdownPolicyEngine, MarkdownPolicyEngine>();
        services.AddScoped<ILotStockService, LotStockService>();
        services.AddScoped<IUnitBarcodeStockService, UnitBarcodeStockService>();
        services.AddScoped<ApprovalWorkflowHelper>();
        services.AddScoped<IInTransitWarehouseService, InTransitWarehouseService>();
        services.AddScoped<IInventoryValuationService, InventoryValuationService>();
        services.AddScoped<IStockReconciliationService, StockReconciliationService>();
        services.AddScoped<IInventoryDocumentAppService, InventoryDocumentAppService>();
        services.AddScoped<ICurrentStockAppService, CurrentStockAppService>();
        services.AddScoped<IVariantUnitBarcodeAppService, VariantUnitBarcodeAppService>();
        services.AddScoped<IStockTransactionAppService, StockTransactionAppService>();
        services.AddScoped<ISalesIntegrationService, SalesIntegrationService>();
        services.AddScoped<IStockReservationService, StockReservationService>();
        services.AddScoped<IWarehouseFulfillmentService, WarehouseFulfillmentService>();
        services.AddScoped<ISupplierAppService, SupplierAppService>();
        services.AddScoped<IPurchaseOrderAppService, PurchaseOrderAppService>();
        services.AddScoped<IGoodsReceiptAppService, GoodsReceiptAppService>();
        services.AddScoped<IAnalyticsAppService, AnalyticsAppService>();
        services.AddScoped<IInventoryInsightsAppService, InventoryInsightsAppService>();
        services.AddScoped<IInsightRecommendationEngine, InsightRecommendationEngine>();
        services.AddScoped<IInsightExplainService, InsightExplainService>();
        services.AddScoped<IMarkdownWhatIfService, MarkdownWhatIfService>();
        services.AddScoped<IInsightActionAppService, InsightActionAppService>();
        services.AddScoped<IInsightAlertService, InsightAlertService>();
        services.AddScoped<IInventoryInsightsSnapshotService, InventoryInsightsSnapshotService>();
        services.AddSingleton<IBackgroundJobCoordinator, BackgroundJobCoordinator>();
        services.AddScoped<IBackgroundJobExecutor, BackgroundJobExecutor>();
        services.AddScoped<IAdminOperationsAppService, AdminOperationsAppService>();
        services.AddScoped<IStockReservationQueryAppService, StockReservationQueryAppService>();
        services.AddScoped<IInventoryReportsAppService, InventoryReportsAppService>();
        services.AddScoped<IFbDataSeedService, FbDataSeedService>();
        services.AddScoped<IDemoUserSeedService, DemoUserSeedService>();

        if (configuration is not null)
        {
            services.AddStockLedgerRetailCaching(configuration);
            services.Configure<FbDataSeedOptions>(
                configuration.GetSection(FbDataSeedOptions.SectionName));
            services.Configure<ApprovalWorkflowOptions>(
                configuration.GetSection(ApprovalWorkflowOptions.SectionName));
            services.Configure<SalesIntegrationOptions>(
                configuration.GetSection(SalesIntegrationOptions.SectionName));
            services.Configure<StockReconciliationOptions>(
                configuration.GetSection(StockReconciliationOptions.SectionName));
            services.Configure<InsightSnapshotOptions>(
                configuration.GetSection(InsightSnapshotOptions.SectionName));
            services.Configure<InsightAlertOptions>(
                configuration.GetSection(InsightAlertOptions.SectionName));
            services.Configure<LoginOptions>(
                configuration.GetSection(LoginOptions.SectionName));
        }

        return services;
    }
}
