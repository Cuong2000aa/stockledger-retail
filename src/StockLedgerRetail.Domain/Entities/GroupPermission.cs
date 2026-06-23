namespace StockLedgerRetail.Domain.Entities;

public class GroupPermission
{
    public Guid GroupId { get; set; }

    public Guid PermissionId { get; set; }

    public PermissionGroup Group { get; set; } = null!;

    public Permission Permission { get; set; } = null!;
}
