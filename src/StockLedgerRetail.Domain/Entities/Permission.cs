namespace StockLedgerRetail.Domain.Entities;

public class Permission
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public ICollection<GroupPermission> Groups { get; set; } = new List<GroupPermission>();
}
