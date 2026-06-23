namespace StockLedgerRetail.Domain.Entities;

public class Team : AuditedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid LeaderUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public AppUser Leader { get; set; } = null!;

    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}
