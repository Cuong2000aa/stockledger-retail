namespace StockLedgerRetail.Domain.Entities;

public class UserGroupAssignment
{
    public Guid UserId { get; set; }

    public Guid GroupId { get; set; }

    public DateTime AssignedAt { get; set; }

    public AppUser User { get; set; } = null!;

    public PermissionGroup Group { get; set; } = null!;
}
