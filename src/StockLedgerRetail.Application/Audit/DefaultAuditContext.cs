namespace StockLedgerRetail.Audit;

public class DefaultAuditContext : IAuditContext
{
    public string UserName => "system";

    public string? IpAddress => null;
}
