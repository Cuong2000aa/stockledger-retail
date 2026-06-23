using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Team>> GetListAsync(CancellationToken cancellationToken = default);

    Task<bool> IsLeaderOfMemberAsync(Guid leaderUserId, string memberEmail, CancellationToken cancellationToken = default);

    Task InsertAsync(Team team, CancellationToken cancellationToken = default);

    Task UpdateAsync(Team team, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
