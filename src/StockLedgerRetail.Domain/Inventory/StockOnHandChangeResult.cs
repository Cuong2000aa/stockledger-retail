namespace StockLedgerRetail.Domain.Inventory;

public sealed record StockOnHandChangeResult(
    Guid CurrentStockId,
    decimal BeforeOnHand,
    decimal AfterOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable);
