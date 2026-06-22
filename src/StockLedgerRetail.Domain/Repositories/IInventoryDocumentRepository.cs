using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface IInventoryDocumentRepository
{
    Task<InventoryDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventoryDocument?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventoryDocument?> GetBySourceReferenceAsync(
        string sourceSystem,
        string referenceNo,
        InventoryDocumentType documentType,
        CancellationToken cancellationToken = default);

    Task<InventoryDocument?> GetByDocumentNoAsync(string documentNo, CancellationToken cancellationToken = default);

    Task<List<InventoryDocument>> GetListAsync(
        InventoryDocumentType? documentType = null,
        CancellationToken cancellationToken = default);

    Task<(List<InventoryDocument> Items, int TotalCount)> GetPagedListAsync(
        InventoryDocumentType? documentType,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountByTypeAndDatePrefixAsync(
        InventoryDocumentType documentType,
        string datePrefix,
        CancellationToken cancellationToken = default);

    Task InsertAsync(InventoryDocument document, CancellationToken cancellationToken = default);

    Task UpdateAsync(InventoryDocument document, CancellationToken cancellationToken = default);

    Task RemoveLinesByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
