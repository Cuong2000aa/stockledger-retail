using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.RegionCode).HasMaxLength(20);
        builder.Property(x => x.AddressLine).HasMaxLength(300);
        builder.Property(x => x.Ward).HasMaxLength(100);
        builder.Property(x => x.District).HasMaxLength(100);
        builder.Property(x => x.Province).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.ContactName).HasMaxLength(150);
        builder.Property(x => x.FullAddress).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.Type, x.BrandId });

        builder.HasOne(x => x.Brand)
            .WithMany(x => x.Warehouses)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentWarehouse)
            .WithMany(x => x.ChildWarehouses)
            .HasForeignKey(x => x.ParentWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
