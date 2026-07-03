using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class InventoryDailyRollupConfiguration : IEntityTypeConfiguration<InventoryDailyRollup>
{
    public void Configure(EntityTypeBuilder<InventoryDailyRollup> builder)
    {
        builder.ToTable("inventory_daily_rollups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RegionCode).HasMaxLength(20);
        builder.Property(x => x.TotalOnHand).HasPrecision(18, 4);
        builder.Property(x => x.TotalAvailable).HasPrecision(18, 4);
        builder.Property(x => x.TotalInventoryValue).HasPrecision(18, 4);
        builder.Property(x => x.OutboundQty30d).HasPrecision(18, 4);
        builder.Property(x => x.GeneratedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.SnapshotDate, x.BrandId, x.WarehouseId, x.RegionCode })
            .IsUnique();
        builder.HasIndex(x => new { x.BrandId, x.SnapshotDate });
    }
}
