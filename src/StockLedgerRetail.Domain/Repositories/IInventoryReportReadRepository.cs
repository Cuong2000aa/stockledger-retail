namespace StockLedgerRetail.Domain.Repositories;

public class InventoryValueLineReadModel
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal InventoryValue { get; set; }
}

public class NxtMovementLineReadModel
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal OpeningQuantity { get; set; }

    public decimal InQuantity { get; set; }

    public decimal OutQuantity { get; set; }

    public decimal ClosingQuantity { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal OpeningValue { get; set; }

    public decimal InValue { get; set; }

    public decimal OutValue { get; set; }

    public decimal ClosingValue { get; set; }
}

public class NxtReportTotalsReadModel
{
    public decimal TotalOpeningValue { get; set; }

    public decimal TotalInValue { get; set; }

    public decimal TotalOutValue { get; set; }

    public decimal TotalClosingValue { get; set; }

    public int TotalLineCount { get; set; }
}

public interface IInventoryReportReadRepository
{
    Task<(decimal TotalValue, int TotalLineCount)> GetInventoryValueTotalsAsync(
        Guid? warehouseId,
        Guid? brandId,
        CancellationToken cancellationToken = default);

    Task<List<InventoryValueLineReadModel>> GetInventoryValueLinesAsync(
        Guid? warehouseId,
        Guid? brandId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<NxtReportTotalsReadModel> GetNxtTotalsAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        Guid? warehouseId,
        CancellationToken cancellationToken = default);

    Task<List<NxtMovementLineReadModel>> GetNxtLinesAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        Guid? warehouseId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
