using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants", t =>
        {
            t.HasCheckConstraint(
                "CK_product_variants_cost_price_non_negative",
                "\"CostPrice\" IS NULL OR \"CostPrice\" >= 0");
            t.HasCheckConstraint(
                "CK_product_variants_selling_price_non_negative",
                "\"SellingPrice\" IS NULL OR \"SellingPrice\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sku).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Barcode).HasMaxLength(50);
        builder.Property(x => x.Color).HasMaxLength(50);
        builder.Property(x => x.Size).HasMaxLength(50);
        builder.Property(x => x.Season).HasMaxLength(50);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CostPrice).HasPrecision(18, 4);
        builder.Property(x => x.SellingPrice).HasPrecision(18, 4);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.BrandId, x.Sku }).IsUnique();
        builder.HasIndex(x => x.Barcode).IsUnique();
    }
}
