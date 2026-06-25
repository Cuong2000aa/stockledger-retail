using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class InventoryDocumentLineBarcodeConfiguration : IEntityTypeConfiguration<InventoryDocumentLineBarcode>
{
    public void Configure(EntityTypeBuilder<InventoryDocumentLineBarcode> builder)
    {
        builder.ToTable("inventory_document_line_barcodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => x.InventoryDocumentLineId);
        builder.HasIndex(x => x.Barcode);

        builder.HasOne(x => x.Line)
            .WithMany(x => x.UnitBarcodes)
            .HasForeignKey(x => x.InventoryDocumentLineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
