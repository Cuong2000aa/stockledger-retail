namespace StockLedgerRetail.Domain.Entities;

/// <summary>Precomputed insight payload for fast API reads.</summary>
public class InsightSnapshot
{
    public Guid Id { get; set; }

    public string SnapshotKey { get; set; } = string.Empty;

    public string InsightKind { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime GeneratedAtUtc { get; set; }
}
