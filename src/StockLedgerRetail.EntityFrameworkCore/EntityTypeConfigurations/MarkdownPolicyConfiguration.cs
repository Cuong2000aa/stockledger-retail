using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class MarkdownPolicyConfiguration : IEntityTypeConfiguration<MarkdownPolicy>
{
    public void Configure(EntityTypeBuilder<MarkdownPolicy> builder)
    {
        builder.ToTable("markdown_policies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RegionCode).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.TiersJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.MinOnHand).HasPrecision(18, 4);
        builder.Property(x => x.MinInventoryValueAtCost).HasPrecision(18, 4);
        builder.Property(x => x.MinGrossMarginPercent).HasPrecision(9, 4);
        builder.Property(x => x.MaxMarkdownPercent).HasPrecision(9, 4);
        builder.Property(x => x.RequireApprovalAbovePercent).HasPrecision(9, 4);
        builder.Property(x => x.SlowSellThroughThreshold).HasPrecision(9, 4);

        builder.HasOne(x => x.Brand)
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BrandId, x.RegionCode, x.WarehouseType, x.IsActive });
    }
}
