namespace StockLedgerRetail.Domain.Entities;

public class BackgroundJobSetting
{
    public Guid Id { get; set; }

    public string JobKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 30;

    public string LastStatus { get; set; } = "idle";

    public string? LastMessage { get; set; }

    public DateTime? LastRunStartedAtUtc { get; set; }

    public DateTime? LastRunCompletedAtUtc { get; set; }

    public DateTime? NextRunAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public class BackgroundJobRun
{
    public Guid Id { get; set; }

    public string JobKey { get; set; } = string.Empty;

    public string TriggeredBy { get; set; } = "scheduled";

    public string Status { get; set; } = "running";

    public string? Message { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public long? DurationMs { get; set; }
}
