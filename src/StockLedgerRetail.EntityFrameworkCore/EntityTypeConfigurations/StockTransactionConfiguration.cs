using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("stock_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionNo).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TransactionType).IsRequired();
        builder.Property(x => x.QuantityDelta).HasPrecision(18, 4);
        builder.Property(x => x.BeforeQuantity).HasPrecision(18, 4);
        builder.Property(x => x.AfterQuantity).HasPrecision(18, 4);
        builder.Property(x => x.TransactionDate).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.TransactionNo).IsUnique();
        builder.HasIndex(x => x.ProductVariantId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.TransactionDate);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.DocumentLineId);

        builder.HasOne(x => x.Document)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DocumentLine)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.DocumentLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
