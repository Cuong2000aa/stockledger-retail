using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StockLedgerRetail.Application.Integration;
using StockLedgerRetail.Application.Inventory;
using StockLedgerRetail.Application.ProductVariants;
using StockLedgerRetail.Application.Products;
using StockLedgerRetail.Application.Warehouses;
using StockLedgerRetail.Audit;
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
        services.AddScoped<IInventoryDocumentAppService, InventoryDocumentAppService>();
        services.AddScoped<ICurrentStockAppService, CurrentStockAppService>();
        services.AddScoped<IStockTransactionAppService, StockTransactionAppService>();
        services.AddScoped<ISalesIntegrationService, SalesIntegrationService>();

        if (configuration is not null)
        {
            services.Configure<SalesIntegrationOptions>(
                configuration.GetSection(SalesIntegrationOptions.SectionName));
        }

        return services;
    }
}
