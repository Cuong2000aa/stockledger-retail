using StockLedgerRetail.Application.Analytics;
using StockLedgerRetail.Enums;
using Xunit;

namespace StockLedgerRetail.Application.Tests;

public class MovementMetricsTests
{
    [Theory]
    [InlineData(StockTransactionType.In, true, false, false)]
    [InlineData(StockTransactionType.Out, false, true, false)]
    [InlineData(StockTransactionType.TransferIn, false, false, true)]
    [InlineData(StockTransactionType.TransferOut, false, false, true)]
    [InlineData(StockTransactionType.AdjustmentIn, true, false, false)]
    [InlineData(StockTransactionType.AdjustmentOut, false, true, false)]
    public void Classifies_transaction_types(
        StockTransactionType type,
        bool operationalIn,
        bool operationalOut,
        bool transfer)
    {
        Assert.Equal(operationalIn, MovementMetrics.IsOperationalIn(type));
        Assert.Equal(operationalOut, MovementMetrics.IsOperationalOut(type));
        Assert.Equal(transfer, MovementMetrics.IsTransfer(type));
    }
}
