using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class InsightActionLogConfiguration : IEntityTypeConfiguration<InsightActionLog>
{
    public void Configure(EntityTypeBuilder<InsightActionLog> builder)
    {
        builder.ToTable("insight_action_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InsightKind).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ActionCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ActionStatus).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("text");
        builder.Property(x => x.ResultEntityType).HasMaxLength(50);
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(50);

        builder.HasIndex(x => x.InsightKind);
        builder.HasIndex(x => x.ActionStatus);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.ProductVariantId, x.WarehouseId });
    }
}
