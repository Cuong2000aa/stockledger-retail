using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class TransferPolicyConfiguration : IEntityTypeConfiguration<TransferPolicy>
{
    public void Configure(EntityTypeBuilder<TransferPolicy> builder)
    {
        builder.ToTable("transfer_policies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.SourceBrand)
            .WithMany()
            .HasForeignKey(x => x.SourceBrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationBrand)
            .WithMany()
            .HasForeignKey(x => x.DestinationBrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SourceBrandId, x.DestinationBrandId, x.IsActive });
    }
}
