using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class UserWarehouseAssignmentConfiguration : IEntityTypeConfiguration<UserWarehouseAssignment>
{
    public void Configure(EntityTypeBuilder<UserWarehouseAssignment> builder)
    {
        builder.ToTable("user_warehouse_assignments");
        builder.HasKey(x => new { x.UserId, x.WarehouseId });
        builder.Property(x => x.AssignedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.WarehouseAssignments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.IsPrimary });
    }
}
