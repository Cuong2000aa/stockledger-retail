using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("product_prices", t =>
        {
            t.HasCheckConstraint(
                "CK_product_prices_before_vat_non_negative",
                "\"PriceBeforeVat\" >= 0");
            t.HasCheckConstraint(
                "CK_product_prices_after_vat_non_negative",
                "\"PriceAfterVat\" >= 0");
            t.HasCheckConstraint(
                "CK_product_prices_vat_rate_non_negative",
                "\"VatRate\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PriceBeforeVat).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.PriceAfterVat).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.VatRate).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.ChannelCode).HasMaxLength(50);
        builder.Property(x => x.ReferenceType).HasMaxLength(50);
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.ProductVariantId);
        builder.HasIndex(x => new { x.ProductVariantId, x.IsCurrent });
        builder.HasIndex(x => new { x.ProductVariantId, x.EffectiveFrom });

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.PriceHistory)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
