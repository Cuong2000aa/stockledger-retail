using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public TeamRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Team?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Teams
            .Include(x => x.Leader)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<Team>> GetListAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Teams
            .Include(x => x.Leader)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

    public Task<bool> IsLeaderOfMemberAsync(
        Guid leaderUserId,
        string memberEmail,
        CancellationToken cancellationToken = default) =>
        _dbContext.Teams
            .AsNoTracking()
            .Where(x => x.IsActive && x.LeaderUserId == leaderUserId)
            .AnyAsync(
                x => x.Members.Any(m => m.User.Email == memberEmail.ToLowerInvariant()),
                cancellationToken);

    public async Task InsertAsync(Team team, CancellationToken cancellationToken = default) =>
        await _dbContext.Teams.AddAsync(team, cancellationToken);

    public Task UpdateAsync(Team team, CancellationToken cancellationToken = default)
    {
        _dbContext.Teams.Update(team);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
