using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<Brand>> GetListAsync(CancellationToken cancellationToken = default);

    Task InsertAsync(Brand brand, CancellationToken cancellationToken = default);

    Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
