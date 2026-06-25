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

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<TransferPolicy> TransferPolicies => Set<TransferPolicy>();

    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<PermissionGroup> PermissionGroups => Set<PermissionGroup>();

    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

    public DbSet<UserGroupAssignment> UserGroupAssignments => Set<UserGroupAssignment>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductCostHistory> ProductCostHistories => Set<ProductCostHistory>();

    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    public DbSet<InventoryValuationSnapshot> InventoryValuationSnapshots => Set<InventoryValuationSnapshot>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<CurrentStock> CurrentStocks => Set<CurrentStock>();

    public DbSet<InventoryDocument> InventoryDocuments => Set<InventoryDocument>();

    public DbSet<InventoryDocumentLine> InventoryDocumentLines => Set<InventoryDocumentLine>();

    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    public DbSet<TransactionLog> TransactionLogs => Set<TransactionLog>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();

    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    public DbSet<StockReservationLine> StockReservationLines => Set<StockReservationLine>();

    public DbSet<InsightSnapshot> InsightSnapshots => Set<InsightSnapshot>();

    public DbSet<BackgroundJobSetting> BackgroundJobSettings => Set<BackgroundJobSetting>();

    public DbSet<BackgroundJobRun> BackgroundJobRuns => Set<BackgroundJobRun>();

    public DbSet<StockLot> StockLots => Set<StockLot>();

    public DbSet<LotStock> LotStocks => Set<LotStock>();

    public DbSet<VariantUnitBarcode> VariantUnitBarcodes => Set<VariantUnitBarcode>();

    public DbSet<InventoryDocumentLineBarcode> InventoryDocumentLineBarcodes =>
        Set<InventoryDocumentLineBarcode>();

    public DbSet<PurchaseOrderLineBarcode> PurchaseOrderLineBarcodes => Set<PurchaseOrderLineBarcode>();

    public DbSet<GoodsReceiptLineBarcode> GoodsReceiptLineBarcodes => Set<GoodsReceiptLineBarcode>();

    public DbSet<StockTransactionBarcode> StockTransactionBarcodes => Set<StockTransactionBarcode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockLedgerRetailDbContext).Assembly);
    }
}
