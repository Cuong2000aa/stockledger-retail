using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public AppUserRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.AppUsers
            .Include(x => x.GroupAssignments)
            .ThenInclude(x => x.Group)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.AppUsers.FirstOrDefaultAsync(
            x => x.Email == email.ToLowerInvariant(),
            cancellationToken);

    public Task<AppUser?> GetByEmailWithPermissionsAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.AppUsers
            .AsNoTracking()
            .Include(x => x.GroupAssignments)
            .ThenInclude(x => x.Group)
            .ThenInclude(x => x.Permissions)
            .ThenInclude(x => x.Permission)
            .Include(x => x.WarehouseAssignments)
            .FirstOrDefaultAsync(x => x.Email == email.ToLowerInvariant(), cancellationToken);

    public Task<AppUser?> GetByIdWithAssignmentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.AppUsers
            .Include(x => x.GroupAssignments)
            .ThenInclude(x => x.Group)
            .Include(x => x.WarehouseAssignments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<AppUser>> GetListAsync(CancellationToken cancellationToken = default) =>
        _dbContext.AppUsers
            .Include(x => x.GroupAssignments)
            .ThenInclude(x => x.Group)
            .Include(x => x.WarehouseAssignments)
            .OrderBy(x => x.Email)
            .ToListAsync(cancellationToken);

    public async Task InsertAsync(AppUser user, CancellationToken cancellationToken = default) =>
        await _dbContext.AppUsers.AddAsync(user, cancellationToken);

    public Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        _dbContext.AppUsers.Update(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
