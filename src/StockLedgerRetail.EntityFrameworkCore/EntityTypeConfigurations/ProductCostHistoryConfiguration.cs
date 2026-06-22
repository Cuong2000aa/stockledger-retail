using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class ProductCostHistoryConfiguration : IEntityTypeConfiguration<ProductCostHistory>
{
    public void Configure(EntityTypeBuilder<ProductCostHistory> builder)
    {
        builder.ToTable("product_cost_histories", t =>
        {
            t.HasCheckConstraint(
                "CK_product_cost_history_cost_price_non_negative",
                "\"CostPrice\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CostPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CostSource).IsRequired();
        builder.Property(x => x.EffectiveFrom).IsRequired();

        builder.HasIndex(x => x.ProductVariantId);
        builder.HasIndex(x => new { x.ProductVariantId, x.EffectiveFrom });

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.CostHistory)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
