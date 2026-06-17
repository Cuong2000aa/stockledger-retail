using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class InventoryDocumentRepository : IInventoryDocumentRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public InventoryDocumentRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<InventoryDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryDocuments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<InventoryDocument?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryDocuments
            .Include(x => x.Lines)
                .ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<InventoryDocument?> GetBySourceReferenceAsync(
        string sourceSystem,
        string referenceNo,
        InventoryDocumentType documentType,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryDocuments
            .Include(x => x.Lines)
                .ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(
                x => x.SourceSystem == sourceSystem
                    && x.ReferenceNo == referenceNo
                    && x.DocumentType == documentType,
                cancellationToken);

    public Task<InventoryDocument?> GetByDocumentNoAsync(string documentNo, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryDocuments.FirstOrDefaultAsync(x => x.DocumentNo == documentNo, cancellationToken);

    public Task<List<InventoryDocument>> GetListAsync(
        InventoryDocumentType? documentType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InventoryDocuments.AsQueryable();

        if (documentType.HasValue)
        {
            query = query.Where(x => x.DocumentType == documentType.Value);
        }

        return query.OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<int> CountByTypeAndDatePrefixAsync(
        InventoryDocumentType documentType,
        string datePrefix,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryDocuments
            .CountAsync(
                x => x.DocumentType == documentType && x.DocumentNo.StartsWith(datePrefix),
                cancellationToken);

    public async Task InsertAsync(InventoryDocument document, CancellationToken cancellationToken = default) =>
        await _dbContext.InventoryDocuments.AddAsync(document, cancellationToken);

    public Task UpdateAsync(InventoryDocument document, CancellationToken cancellationToken = default)
    {
        _dbContext.InventoryDocuments.Update(document);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
