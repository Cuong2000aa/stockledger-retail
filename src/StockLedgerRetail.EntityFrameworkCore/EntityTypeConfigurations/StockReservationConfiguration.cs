using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReservationNo).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SourceSystem).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ReferenceKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ReferenceType).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.ReservationNo).IsUnique();
        builder.HasIndex(x => new { x.SourceSystem, x.ReferenceType, x.ReferenceKey, x.WarehouseId, x.Status });

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.StockReservation)
            .HasForeignKey(x => x.StockReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
