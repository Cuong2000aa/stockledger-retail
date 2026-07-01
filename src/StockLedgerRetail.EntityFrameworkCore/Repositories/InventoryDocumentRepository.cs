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
            .Include(x => x.Lines)
                .ThenInclude(l => l.UnitBarcodes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<InventoryDocument?> GetBySourceReferenceAsync(
        string sourceSystem,
        string referenceNo,
        InventoryDocumentType documentType,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryDocuments
            .Include(x => x.Lines)
                .ThenInclude(l => l.ProductVariant)
            .Include(x => x.Lines)
                .ThenInclude(l => l.UnitBarcodes)
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

    public async Task<(List<InventoryDocument> Items, int TotalCount)> GetPagedListAsync(
        InventoryDocumentType? documentType,
        InventoryDocumentStatus? status,
        TransferLifecycleStatus? transferLifecycle,
        int skip,
        int take,
        string? search = null,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        if (scopedWarehouseIds is { Count: 0 })
        {
            return ([], 0);
        }

        var query = _dbContext.InventoryDocuments.AsQueryable();

        if (documentType.HasValue)
        {
            query = query.Where(x => x.DocumentType == documentType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (transferLifecycle.HasValue)
        {
            query = query.Where(x => x.TransferLifecycleStatus == transferLifecycle.Value);
        }

        if (scopedWarehouseIds is { Count: > 0 })
        {
            query = query.Where(x =>
                (x.SourceWarehouseId.HasValue && scopedWarehouseIds.Contains(x.SourceWarehouseId.Value))
                || (x.DestinationWarehouseId.HasValue && scopedWarehouseIds.Contains(x.DestinationWarehouseId.Value)));
        }

        var term = TextSearchHelper.Normalize(search);
        if (term is not null)
        {
            var pattern = TextSearchHelper.ToLikePattern(term);
            query = query.Where(x =>
                EF.Functions.ILike(x.DocumentNo, pattern) ||
                (x.ReferenceNo != null && EF.Functions.ILike(x.ReferenceNo, pattern)));
        }

        query = query.OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
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
        if (_dbContext.Entry(document).State == EntityState.Detached)
        {
            _dbContext.InventoryDocuments.Update(document);
        }

        return Task.CompletedTask;
    }

    public Task RemoveLinesByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryDocumentLines
            .Where(x => x.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
