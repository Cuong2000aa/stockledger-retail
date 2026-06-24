using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.EntityFrameworkCore.EntityTypeConfigurations;

public class BackgroundJobSettingConfiguration : IEntityTypeConfiguration<BackgroundJobSetting>
{
    public void Configure(EntityTypeBuilder<BackgroundJobSetting> builder)
    {
        builder.ToTable("background_job_settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.LastStatus).IsRequired().HasMaxLength(30);
        builder.Property(x => x.LastMessage).HasMaxLength(2000);
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.JobKey).IsUnique();
    }
}

public class BackgroundJobRunConfiguration : IEntityTypeConfiguration<BackgroundJobRun>
{
    public void Configure(EntityTypeBuilder<BackgroundJobRun> builder)
    {
        builder.ToTable("background_job_runs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.TriggeredBy).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.StartedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.JobKey, x.StartedAtUtc });
    }
}
