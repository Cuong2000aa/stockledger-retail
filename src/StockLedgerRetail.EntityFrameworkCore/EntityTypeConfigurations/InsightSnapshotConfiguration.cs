using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class InsightSnapshotConfiguration : IEntityTypeConfiguration<InsightSnapshot>
{
    public void Configure(EntityTypeBuilder<InsightSnapshot> builder)
    {
        builder.ToTable("insight_snapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SnapshotKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.InsightKind).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.GeneratedAtUtc).IsRequired();

        builder.HasIndex(x => x.SnapshotKey).IsUnique();
        builder.HasIndex(x => new { x.InsightKind, x.GeneratedAtUtc });
    }
}
