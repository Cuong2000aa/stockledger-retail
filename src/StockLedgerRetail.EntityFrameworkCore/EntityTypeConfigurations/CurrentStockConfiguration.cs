using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class CurrentStockConfiguration : IEntityTypeConfiguration<CurrentStock>
{
    public void Configure(EntityTypeBuilder<CurrentStock> builder)
    {
        builder.ToTable("current_stocks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityOnHand).HasPrecision(18, 4);
        builder.Property(x => x.QuantityReserved).HasPrecision(18, 4);
        builder.Property(x => x.QuantityAvailable).HasPrecision(18, 4);
        builder.Property(x => x.LastUpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.ProductVariantId, x.WarehouseId }).IsUnique();

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.CurrentStocks)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.CurrentStocks)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LastTransaction)
            .WithMany(x => x.CurrentStocks)
            .HasForeignKey(x => x.LastTransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
