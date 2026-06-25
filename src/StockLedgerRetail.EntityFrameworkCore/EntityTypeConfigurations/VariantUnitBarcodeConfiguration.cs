using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class VariantUnitBarcodeConfiguration : IEntityTypeConfiguration<VariantUnitBarcode>
{
    public void Configure(EntityTypeBuilder<VariantUnitBarcode> builder)
    {
        builder.ToTable("variant_unit_barcodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.Property(x => x.LastUpdatedAt).IsRequired();

        builder.HasIndex(x => x.Barcode).IsUnique();
        builder.HasIndex(x => new { x.ProductVariantId, x.WarehouseId, x.Status });
        builder.HasIndex(x => x.ProductVariantId);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
