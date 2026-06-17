using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.EntityFrameworkCore.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore;

public static class StockLedgerRetailEntityFrameworkCoreModule
{
    public static IServiceCollection AddStockLedgerRetailEntityFrameworkCore(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<StockLedgerRetailDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<ITransactionLogRepository, TransactionLogRepository>();

        return services;
    }
}
