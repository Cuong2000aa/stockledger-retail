namespace StockLedgerRetail.Domain.Entities;

public class PermissionGroup : AuditedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<GroupPermission> Permissions { get; set; } = new List<GroupPermission>();

    public ICollection<UserGroupAssignment> UserAssignments { get; set; } = new List<UserGroupAssignment>();
}
