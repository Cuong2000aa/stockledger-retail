using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore;

public class StockLedgerRetailDbContext : DbContext
{
    public StockLedgerRetailDbContext(DbContextOptions<StockLedgerRetailDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<CurrentStock> CurrentStocks => Set<CurrentStock>();

    public DbSet<InventoryDocument> InventoryDocuments => Set<InventoryDocument>();

    public DbSet<InventoryDocumentLine> InventoryDocumentLines => Set<InventoryDocumentLine>();

    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    public DbSet<TransactionLog> TransactionLogs => Set<TransactionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockLedgerRetailDbContext).Assembly);
    }
}
