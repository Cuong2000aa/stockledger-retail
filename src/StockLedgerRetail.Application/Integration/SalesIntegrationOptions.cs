namespace StockLedgerRetail.Application.Integration;

public class SalesIntegrationOptions
{
    public const string SectionName = "Integration:Sales";

    public string DefaultSourceSystem { get; set; } = "POS";

    public List<string> AllowedSourceSystems { get; set; } = new()
    {
        "POS",
        "OMS",
        "ECOM",
        "ERP"
    };

    public string? ApiKey { get; set; }

    /// <summary>Default reservation TTL in minutes for cart/order holds.</summary>
    public int ReservationExpiryMinutes { get; set; } = 30;
}
