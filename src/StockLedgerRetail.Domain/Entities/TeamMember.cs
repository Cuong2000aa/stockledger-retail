namespace StockLedgerRetail.Domain.Entities;

public class TeamMember
{
    public Guid TeamId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public Team Team { get; set; } = null!;

    public AppUser User { get; set; } = null!;
}
