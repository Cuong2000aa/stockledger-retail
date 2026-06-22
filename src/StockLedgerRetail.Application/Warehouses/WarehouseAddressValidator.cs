using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Application.Warehouses;

public static class WarehouseAddressValidator
{
    public static void EnsureValidForType(
        WarehouseType type,
        string? addressLine,
        string? ward,
        string? district,
        string? province)
    {
        if (type is not (WarehouseType.Dc or WarehouseType.Store))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(addressLine))
        {
            throw new InvalidOperationException("Address line is required for DC and store warehouses.");
        }

        if (string.IsNullOrWhiteSpace(ward))
        {
            throw new InvalidOperationException("Ward is required for DC and store warehouses.");
        }

        if (string.IsNullOrWhiteSpace(district))
        {
            throw new InvalidOperationException("District is required for DC and store warehouses.");
        }

        if (string.IsNullOrWhiteSpace(province))
        {
            throw new InvalidOperationException("Province is required for DC and store warehouses.");
        }
    }
}
