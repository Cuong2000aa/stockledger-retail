namespace StockLedgerRetail.Services;

public interface ITransferPolicyService
{
    Task ValidateTransferAsync(
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        IReadOnlyCollection<Guid> productVariantIds,
        CancellationToken cancellationToken = default);
}

public interface IInTransitWarehouseService
{
    Task<Guid> GetOrCreateInTransitWarehouseIdAsync(
        Guid? brandId,
        CancellationToken cancellationToken = default);
}
