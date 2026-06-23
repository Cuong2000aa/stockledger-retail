using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.EntityFrameworkCore.Infrastructure;
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
        services.AddScoped<IInventoryDocumentRepository, InventoryDocumentRepository>();
        services.AddScoped<ICurrentStockRepository, CurrentStockRepository>();
        services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();
        services.AddScoped<IInventoryInsightReadRepository, InventoryInsightReadRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        services.AddScoped<IProductCostHistoryRepository, ProductCostHistoryRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();

        return services;
    }
}
