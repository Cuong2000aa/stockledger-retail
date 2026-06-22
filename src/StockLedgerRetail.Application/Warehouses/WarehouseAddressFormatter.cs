namespace StockLedgerRetail.Application.Warehouses;

public static class WarehouseAddressFormatter
{
    public const int MaxFullAddressLength = 1000;

    public static string? BuildFullAddress(
        string? addressLine,
        string? ward,
        string? district,
        string? province,
        string? postalCode)
    {
        var fullAddress = FormatFullAddress(addressLine, ward, district, province, postalCode);
        if (fullAddress is not null && fullAddress.Length > MaxFullAddressLength)
        {
            throw new InvalidOperationException(
                $"Full address cannot exceed {MaxFullAddressLength} characters.");
        }

        return fullAddress;
    }

    public static string? FormatFullAddress(
        string? addressLine,
        string? ward,
        string? district,
        string? province,
        string? postalCode)
    {
        var locality = JoinParts(ward, district, province);
        var core = JoinParts(
            addressLine?.Trim(),
            string.IsNullOrWhiteSpace(locality) ? null : locality);

        if (string.IsNullOrWhiteSpace(core))
        {
            return null;
        }

        var postal = postalCode?.Trim();
        return string.IsNullOrWhiteSpace(postal) ? core : $"{core}, {postal}";
    }

    private static string? JoinParts(params string?[] parts)
    {
        var values = parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();

        return values.Length == 0 ? null : string.Join(", ", values);
    }
}
