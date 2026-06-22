using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class StockReservationLineConfiguration : IEntityTypeConfiguration<StockReservationLine>
{
    public void Configure(EntityTypeBuilder<StockReservationLine> builder)
    {
        builder.ToTable("stock_reservation_lines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(18, 4);

        builder.HasIndex(x => new { x.StockReservationId, x.ProductVariantId }).IsUnique();

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
