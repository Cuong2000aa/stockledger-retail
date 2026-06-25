using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class GoodsReceiptLineBarcodeConfiguration : IEntityTypeConfiguration<GoodsReceiptLineBarcode>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLineBarcode> builder)
    {
        builder.ToTable("goods_receipt_line_barcodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => x.GoodsReceiptLineId);
        builder.HasIndex(x => x.Barcode);

        builder.HasOne(x => x.GoodsReceiptLine)
            .WithMany(x => x.UnitBarcodes)
            .HasForeignKey(x => x.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
