using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class StockLotConfiguration : IEntityTypeConfiguration<StockLot>
{
    public void Configure(EntityTypeBuilder<StockLot> builder)
    {
        builder.ToTable("stock_lots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LotCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.ProductVariantId, x.LotCode }).IsUnique();
        builder.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId);
    }
}
