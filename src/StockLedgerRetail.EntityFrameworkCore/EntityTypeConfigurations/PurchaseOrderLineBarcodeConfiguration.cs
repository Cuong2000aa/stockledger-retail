using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class PurchaseOrderLineBarcodeConfiguration : IEntityTypeConfiguration<PurchaseOrderLineBarcode>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLineBarcode> builder)
    {
        builder.ToTable("purchase_order_line_barcodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => x.PurchaseOrderLineId);
        builder.HasIndex(x => x.Barcode);

        builder.HasOne(x => x.PurchaseOrderLine)
            .WithMany(x => x.UnitBarcodes)
            .HasForeignKey(x => x.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
