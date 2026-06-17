using Microsoft.Extensions.DependencyInjection;
using StockLedgerRetail.Application.ProductVariants;
using StockLedgerRetail.Application.Products;
using StockLedgerRetail.Application.Warehouses;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Services;

namespace StockLedgerRetail;

public static class StockLedgerRetailApplicationModule
{
    public static IServiceCollection AddStockLedgerRetailApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuditContext, DefaultAuditContext>();
        services.AddScoped<ITransactionAuditService, TransactionAuditService>();
        services.AddScoped<IProductAppService, ProductAppService>();
        services.AddScoped<IProductVariantAppService, ProductVariantAppService>();
        services.AddScoped<IWarehouseAppService, WarehouseAppService>();

        return services;
    }
}
