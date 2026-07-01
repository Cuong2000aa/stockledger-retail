using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Application.Analytics;

/// <summary>
/// Classifies stock transactions for dashboard movement KPIs.
/// Transfers are excluded from total in/out because chain-wide on-hand is unchanged.
/// </summary>
public static class MovementMetrics
{
    public static bool IsOperationalIn(StockTransactionType type) =>
        type is StockTransactionType.In
            or StockTransactionType.AdjustmentIn
            or StockTransactionType.CountAdjustmentIn;

    public static bool IsOperationalOut(StockTransactionType type) =>
        type is StockTransactionType.Out
            or StockTransactionType.AdjustmentOut
            or StockTransactionType.CountAdjustmentOut;

    public static bool IsTransfer(StockTransactionType type) =>
        type is StockTransactionType.TransferIn or StockTransactionType.TransferOut;
}
