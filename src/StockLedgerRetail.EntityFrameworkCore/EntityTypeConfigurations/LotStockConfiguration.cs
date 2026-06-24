using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class LotStockConfiguration : IEntityTypeConfiguration<LotStock>
{
    public void Configure(EntityTypeBuilder<LotStock> builder)
    {
        builder.ToTable("lot_stocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityOnHand).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.StockLotId, x.WarehouseId }).IsUnique();
        builder.HasOne(x => x.StockLot).WithMany(x => x.LotStocks).HasForeignKey(x => x.StockLotId);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId);
    }
}
