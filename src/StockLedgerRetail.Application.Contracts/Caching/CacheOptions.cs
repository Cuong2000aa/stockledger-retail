namespace StockLedgerRetail.Caching;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Ghi log HIT/MISS/STORE ở mức Debug. Bật khi cần trace cache vs database.
    /// </summary>
    public bool LogOperations { get; set; }

    public int AuthTtlMinutes { get; set; } = 10;

    public int MasterDataTtlMinutes { get; set; } = 30;

    public int ReportTtlMinutes { get; set; } = 15;

    public int ReportCurrentPeriodTtlMinutes { get; set; } = 2;
}

public class RedisOptions
{
    public const string SectionName = "Redis";

    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = "localhost:6379";

    public string InstanceName { get; set; } = "stockledger:";
}
