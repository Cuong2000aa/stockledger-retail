using StockLedgerRetail.Application.Integration;

namespace StockLedgerRetail.Application.Integration;

public static class IntegrationSourceNormalizer
{
    public static string Normalize(string? sourceSystem, SalesIntegrationOptions options)
    {
        var normalized = string.IsNullOrWhiteSpace(sourceSystem)
            ? options.DefaultSourceSystem
            : sourceSystem.Trim().ToUpperInvariant();

        if (options.AllowedSourceSystems.Count > 0
            && !options.AllowedSourceSystems.Any(x => x.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Source system '{normalized}' is not allowed.");
        }

        return normalized;
    }
}
