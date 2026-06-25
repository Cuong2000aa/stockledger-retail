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
            t.HasCheckConstraint(
                "CK_product_variants_current_cost_price_non_negative",
                "\"CurrentCostPrice\" IS NULL OR \"CurrentCostPrice\" >= 0");
            t.HasCheckConstraint(
                "CK_product_variants_current_selling_price_non_negative",
                "\"CurrentSellingPrice\" IS NULL OR \"CurrentSellingPrice\" >= 0");
            t.HasCheckConstraint(
                "CK_product_variants_vat_rate_non_negative",
                "\"VatRate\" IS NULL OR \"VatRate\" >= 0");
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
        builder.Property(x => x.CurrentCostPrice).HasPrecision(18, 4);
        builder.Property(x => x.CurrentSellingPrice).HasPrecision(18, 4);
        builder.Property(x => x.CurrentSellingPriceBeforeVat).HasPrecision(18, 4);
        builder.Property(x => x.CurrentSellingPriceAfterVat).HasPrecision(18, 4);
        builder.Property(x => x.VatRate).HasPrecision(5, 2);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.BrandId, x.Sku }).IsUnique();
        builder.HasIndex(x => x.Barcode).IsUnique();
    }
}
