namespace StockLedgerRetail.Application.Reports;

/// <summary>
/// Khoảng ngày báo cáo đã chuẩn hóa cho truy vấn SQL.
/// UI gửi fromDate/toDate dạng yyyy-MM-dd (thường lúc 00:00).
/// ToExclusive = ngày sau toDate để gồm cả giao dịch trong ngày cuối kỳ.
/// </summary>
public sealed class ReportDateRange
{
    public DateTime FromInclusiveUtc { get; }

    /// <summary>Biên trên không bao gồm — dùng TransactionDate &lt; ToExclusiveUtc.</summary>
    public DateTime ToExclusiveUtc { get; }

    public DateTime ToDateForDisplay { get; }

    private ReportDateRange(DateTime fromInclusiveUtc, DateTime toExclusiveUtc, DateTime toDateForDisplay)
    {
        FromInclusiveUtc = fromInclusiveUtc;
        ToExclusiveUtc = toExclusiveUtc;
        ToDateForDisplay = toDateForDisplay;
    }

    public static ReportDateRange FromUserInput(DateTime fromDate, DateTime toDate)
    {
        var fromInclusive = fromDate.Date;
        var toDisplay = toDate.Date;
        var toExclusive = toDisplay.AddDays(1);

        if (toExclusive <= fromInclusive)
        {
            throw new ArgumentException(
                "Report toDate must be on or after fromDate.",
                nameof(toDate));
        }

        return new ReportDateRange(fromInclusive, toExclusive, toDisplay);
    }

    public bool IncludesToday() => ToDateForDisplay >= DateTime.UtcNow.Date;
}
