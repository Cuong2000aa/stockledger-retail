namespace StockLedgerRetail.Domain.Entities;

public class AppUser : AuditedEntity
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<UserGroupAssignment> GroupAssignments { get; set; } = new List<UserGroupAssignment>();

    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();

    public ICollection<Team> LedTeams { get; set; } = new List<Team>();
}
