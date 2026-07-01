namespace StockLedgerRetail.EntityFrameworkCore;

public static class TextSearchHelper
{
    public const string LikeEscape = "\\";

    public static string? Normalize(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    public static bool IsExactLookup(string term) =>
        !term.Contains(' ') && term.Length <= 50;

    public static string ToLikePattern(string search)
    {
        var escaped = search
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
        return $"%{escaped}%";
    }
}
