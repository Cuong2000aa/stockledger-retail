namespace StockLedgerRetail.Application.Integration;

public class StockWebhookOptions
{
    public const string SectionName = "Integration:Webhooks";

    public bool Enabled { get; set; } = false;

    /// <summary>List of webhook endpoints (e.g. Shopee sync service, OMS, Ecom).</summary>
    public List<string> Endpoints { get; set; } = new();

    /// <summary>Timeout in seconds for webhook delivery.</summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>Shared secret for HMAC-SHA256 signature verification in X-StockLedger-Signature header.</summary>
    public string? SecretKey { get; set; }
}
