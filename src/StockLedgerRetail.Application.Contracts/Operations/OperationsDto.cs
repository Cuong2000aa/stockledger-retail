namespace StockLedgerRetail.Operations;

public class BackgroundJobDto
{
    public string JobKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; }

    public int IntervalMinutes { get; set; }

    public string LastStatus { get; set; } = BackgroundJobStatuses.Idle;

    public string? LastMessage { get; set; }

    public DateTime? LastRunStartedAtUtc { get; set; }

    public DateTime? LastRunCompletedAtUtc { get; set; }

    public DateTime? NextRunAtUtc { get; set; }

    public bool IsRunning { get; set; }

    public bool ManualRunRequested { get; set; }
}

public class BackgroundJobRunDto
{
    public Guid Id { get; set; }

    public string JobKey { get; set; } = string.Empty;

    public string TriggeredBy { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public long? DurationMs { get; set; }
}

public class OperationsDashboardDto
{
    public List<BackgroundJobDto> Jobs { get; set; } = [];

    public List<BackgroundJobRunDto> RecentRuns { get; set; } = [];
}

public class UpdateBackgroundJobDto
{
    public bool IsEnabled { get; set; }

    public int IntervalMinutes { get; set; }
}

public class TriggerBackgroundJobResponseDto
{
    public string JobKey { get; set; } = string.Empty;

    public bool Accepted { get; set; }

    public string Message { get; set; } = string.Empty;
}
