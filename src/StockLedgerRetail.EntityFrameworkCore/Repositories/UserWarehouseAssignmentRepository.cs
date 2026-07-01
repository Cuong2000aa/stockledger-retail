using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class UserWarehouseAssignmentRepository : IUserWarehouseAssignmentRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public UserWarehouseAssignmentRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<UserWarehouseAssignment>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _dbContext.UserWarehouseAssignments
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.WarehouseId)
            .ToListAsync(cancellationToken);

    public async Task ReplaceForUserAsync(
        Guid userId,
        IReadOnlyCollection<(Guid WarehouseId, bool IsPrimary)> assignments,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserWarehouseAssignments
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        _dbContext.UserWarehouseAssignments.RemoveRange(existing);

        if (assignments.Count == 0)
        {
            return;
        }

        var primaryCount = assignments.Count(x => x.IsPrimary);
        var normalized = assignments
            .GroupBy(x => x.WarehouseId)
            .Select(g => g.First())
            .ToList();

        Guid? primaryId = primaryCount == 1
            ? normalized.First(x => x.IsPrimary).WarehouseId
            : normalized.Count == 1
                ? normalized[0].WarehouseId
                : normalized.FirstOrDefault(x => x.IsPrimary).WarehouseId is var pid && pid != Guid.Empty
                    ? pid
                    : normalized[0].WarehouseId;

        var now = DateTime.UtcNow;
        foreach (var assignment in normalized)
        {
            await _dbContext.UserWarehouseAssignments.AddAsync(
                new UserWarehouseAssignment
                {
                    UserId = userId,
                    WarehouseId = assignment.WarehouseId,
                    IsPrimary = assignment.WarehouseId == primaryId,
                    AssignedAt = now
                },
                cancellationToken);
        }
    }
}
