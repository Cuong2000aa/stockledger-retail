namespace StockLedgerRetail.EntityFrameworkCore;

internal static class TextSearchHelper
{
    public static string? Normalize(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    public static string ToLikePattern(string search) => $"%{search}%";
}
