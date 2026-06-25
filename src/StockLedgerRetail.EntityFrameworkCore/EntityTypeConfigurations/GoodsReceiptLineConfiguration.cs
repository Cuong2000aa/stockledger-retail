using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("goods_receipt_lines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 4);
        builder.Property(x => x.LotCode).HasMaxLength(64);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => x.GoodsReceiptId);
        builder.HasIndex(x => x.PurchaseOrderLineId);
        builder.HasIndex(x => x.ProductVariantId);

        builder.HasOne(x => x.PurchaseOrderLine)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
