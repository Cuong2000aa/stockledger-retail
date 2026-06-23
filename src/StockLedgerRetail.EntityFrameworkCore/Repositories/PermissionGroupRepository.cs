using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class PermissionGroupRepository : IPermissionGroupRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public PermissionGroupRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PermissionGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.PermissionGroups
            .Include(x => x.Permissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PermissionGroup?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.PermissionGroups.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public Task<List<PermissionGroup>> GetListAsync(CancellationToken cancellationToken = default) =>
        _dbContext.PermissionGroups
            .Include(x => x.Permissions)
            .ThenInclude(x => x.Permission)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

    public async Task AssignUserToGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.UserGroupAssignments
            .AnyAsync(x => x.UserId == userId && x.GroupId == groupId, cancellationToken);

        if (exists)
        {
            return;
        }

        await _dbContext.UserGroupAssignments.AddAsync(new UserGroupAssignment
        {
            UserId = userId,
            GroupId = groupId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task RemoveUserFromGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.UserGroupAssignments
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GroupId == groupId, cancellationToken);

        if (assignment is not null)
        {
            _dbContext.UserGroupAssignments.Remove(assignment);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
