namespace StockLedgerRetail.Enums;

/// <summary>
/// Phương pháp định giá tồn kho / giá vốn.
/// </summary>
public enum ValuationMethod
{
    WeightedAverage = 1,
    StandardCost = 2,
    LastPurchase = 3,
    Manual = 4
}
