using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class InventoryValuationSnapshotConfiguration : IEntityTypeConfiguration<InventoryValuationSnapshot>
{
    public void Configure(EntityTypeBuilder<InventoryValuationSnapshot> builder)
    {
        builder.ToTable("inventory_valuation_snapshots", t =>
        {
            t.HasCheckConstraint(
                "CK_inventory_valuation_snapshots_on_hand_non_negative",
                "\"QuantityOnHand\" >= 0");
            t.HasCheckConstraint(
                "CK_inventory_valuation_snapshots_reserved_non_negative",
                "\"QuantityReserved\" >= 0");
            t.HasCheckConstraint(
                "CK_inventory_valuation_snapshots_available_non_negative",
                "\"QuantityAvailable\" >= 0");
            t.HasCheckConstraint(
                "CK_inventory_valuation_snapshots_value_non_negative",
                "\"InventoryValue\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityOnHand).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.QuantityReserved).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.QuantityAvailable).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.AverageCost).HasPrecision(18, 4);
        builder.Property(x => x.InventoryValue).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.ProductVariantId, x.WarehouseId, x.SnapshotDate }).IsUnique();
        builder.HasIndex(x => new { x.WarehouseId, x.SnapshotDate });

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.ValuationSnapshots)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
