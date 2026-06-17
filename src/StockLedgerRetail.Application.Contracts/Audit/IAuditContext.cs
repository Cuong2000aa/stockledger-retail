namespace StockLedgerRetail.Audit;

public interface IAuditContext
{
    string UserName { get; }

    string? IpAddress { get; }
}
