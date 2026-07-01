using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IUserWarehouseAssignmentRepository
{
    Task<List<UserWarehouseAssignment>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task ReplaceForUserAsync(
        Guid userId,
        IReadOnlyCollection<(Guid WarehouseId, bool IsPrimary)> assignments,
        CancellationToken cancellationToken = default);
}
