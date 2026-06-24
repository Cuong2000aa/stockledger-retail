using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Application.Insights;

public static class InsightTransferRules
{
    public static bool CanWarehouseHoldProduct(Guid? warehouseBrandId, Guid? productBrandId)
    {
        if (!productBrandId.HasValue)
        {
            return true;
        }

        if (!warehouseBrandId.HasValue)
        {
            return true;
        }

        return warehouseBrandId == productBrandId;
    }

    public static bool IsRegionCompatible(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return true;
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesBrand(Guid? policyBrandId, Guid? warehouseBrandId) =>
        !policyBrandId.HasValue || policyBrandId == warehouseBrandId;

    public static bool IsCrossBrandTransferAllowed(
        Guid? sourceWarehouseBrandId,
        Guid? destinationWarehouseBrandId,
        IReadOnlyList<TransferPolicy> policies) =>
        policies.Any(policy =>
            policy.IsActive
            && policy.AllowCrossBrand
            && MatchesBrand(policy.SourceBrandId, sourceWarehouseBrandId)
            && MatchesBrand(policy.DestinationBrandId, destinationWarehouseBrandId));

    public static bool CanTransferBetweenWarehouses(
        Guid? sourceWarehouseBrandId,
        Guid? destinationWarehouseBrandId,
        Guid? productBrandId,
        string? sourceRegionCode,
        string? destinationRegionCode,
        IReadOnlyList<TransferPolicy> policies)
    {
        if (!CanWarehouseHoldProduct(sourceWarehouseBrandId, productBrandId)
            || !CanWarehouseHoldProduct(destinationWarehouseBrandId, productBrandId))
        {
            return false;
        }

        if (!IsRegionCompatible(sourceRegionCode, destinationRegionCode))
        {
            return false;
        }

        if (sourceWarehouseBrandId == destinationWarehouseBrandId)
        {
            return true;
        }

        return IsCrossBrandTransferAllowed(sourceWarehouseBrandId, destinationWarehouseBrandId, policies);
    }

    public static Warehouse? FindClearanceDestination(
        IReadOnlyList<Warehouse> warehouses,
        Warehouse sourceWarehouse,
        Guid? productBrandId,
        IReadOnlyList<TransferPolicy> policies)
    {
        return warehouses
            .Where(warehouse => warehouse.Id != sourceWarehouse.Id)
            .Where(warehouse => warehouse.Type is WarehouseType.Defect or WarehouseType.Return or WarehouseType.Dc)
            .Where(warehouse => warehouse.Type != WarehouseType.InTransit)
            .Where(warehouse => CanTransferBetweenWarehouses(
                sourceWarehouse.BrandId,
                warehouse.BrandId,
                productBrandId,
                sourceWarehouse.RegionCode,
                warehouse.RegionCode,
                policies))
            .OrderBy(warehouse => warehouse.Type == WarehouseType.Defect ? 0
                : warehouse.Type == WarehouseType.Return ? 1
                : 2)
            .ThenBy(warehouse => warehouse.Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static bool IsClearanceWarehouse(WarehouseType warehouseType) =>
        warehouseType is WarehouseType.Defect or WarehouseType.Return;
}
