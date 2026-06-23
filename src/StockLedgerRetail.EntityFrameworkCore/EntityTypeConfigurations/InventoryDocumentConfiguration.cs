using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class InventoryDocumentConfiguration : IEntityTypeConfiguration<InventoryDocument>
{
    public void Configure(EntityTypeBuilder<InventoryDocument> builder)
    {
        builder.ToTable("inventory_documents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DocumentType).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.DocumentDate).IsRequired();
        builder.Property(x => x.ReferenceNo).HasMaxLength(100);
        builder.Property(x => x.SourceSystem).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ApprovedBy).HasMaxLength(100);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.DocumentNo).IsUnique();
        builder.HasIndex(x => new { x.SourceSystem, x.ReferenceNo, x.DocumentType })
            .IsUnique()
            .HasFilter("\"ReferenceNo\" IS NOT NULL AND \"SourceSystem\" IS NOT NULL");

        builder.HasOne(x => x.SourceWarehouse)
            .WithMany()
            .HasForeignKey(x => x.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(x => x.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InTransitWarehouse)
            .WithMany()
            .HasForeignKey(x => x.InTransitWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
