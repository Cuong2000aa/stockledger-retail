using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class StockTransactionBarcodeConfiguration : IEntityTypeConfiguration<StockTransactionBarcode>
{
    public void Configure(EntityTypeBuilder<StockTransactionBarcode> builder)
    {
        builder.ToTable("stock_transaction_barcodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => x.StockTransactionId);
        builder.HasIndex(x => x.Barcode);

        builder.HasOne(x => x.StockTransaction)
            .WithMany(x => x.Barcodes)
            .HasForeignKey(x => x.StockTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
