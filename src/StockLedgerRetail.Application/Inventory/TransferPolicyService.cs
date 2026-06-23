using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

public class TransferPolicyService : ITransferPolicyService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly ITransferPolicyRepository _transferPolicyRepository;

    public TransferPolicyService(
        IWarehouseRepository warehouseRepository,
        IProductVariantRepository productVariantRepository,
        ITransferPolicyRepository transferPolicyRepository)
    {
        _warehouseRepository = warehouseRepository;
        _productVariantRepository = productVariantRepository;
        _transferPolicyRepository = transferPolicyRepository;
    }

    public async Task ValidateTransferAsync(
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        IReadOnlyCollection<Guid> productVariantIds,
        CancellationToken cancellationToken = default)
    {
        var source = await _warehouseRepository.GetByIdAsync(sourceWarehouseId, cancellationToken)
            ?? throw new InvalidOperationException($"Source warehouse '{sourceWarehouseId}' was not found.");

        var destination = await _warehouseRepository.GetByIdAsync(destinationWarehouseId, cancellationToken)
            ?? throw new InvalidOperationException($"Destination warehouse '{destinationWarehouseId}' was not found.");

        if (source.Type == WarehouseType.InTransit || destination.Type == WarehouseType.InTransit)
        {
            throw new InvalidOperationException("In-transit warehouses cannot be used as transfer endpoints.");
        }

        var variantBrandIds = await _productVariantRepository.GetBrandIdsByVariantIdsAsync(
            productVariantIds,
            cancellationToken);

        foreach (var variantId in productVariantIds)
        {
            if (!variantBrandIds.TryGetValue(variantId, out var variantBrandId))
            {
                throw new InvalidOperationException($"Product variant '{variantId}' was not found.");
            }

            EnsureWarehouseBrandCompatible(source, variantBrandId, "source");
            EnsureWarehouseBrandCompatible(destination, variantBrandId, "destination");
        }

        if (IsSameBrandScope(source.BrandId, destination.BrandId))
        {
            return;
        }

        var policies = await _transferPolicyRepository.GetActivePoliciesAsync(cancellationToken);
        var allowed = policies.Any(policy =>
            policy.AllowCrossBrand
            && MatchesBrand(policy.SourceBrandId, source.BrandId)
            && MatchesBrand(policy.DestinationBrandId, destination.BrandId));

        if (!allowed)
        {
            throw new InvalidOperationException(
                "Cross-brand transfer is not allowed by transfer policy.");
        }
    }

    private static void EnsureWarehouseBrandCompatible(Warehouse warehouse, Guid? variantBrandId, string role)
    {
        if (!variantBrandId.HasValue)
        {
            return;
        }

        if (warehouse.BrandId.HasValue && warehouse.BrandId != variantBrandId)
        {
            throw new InvalidOperationException(
                $"The {role} warehouse brand does not match product brand.");
        }
    }

    private static bool IsSameBrandScope(Guid? sourceBrandId, Guid? destinationBrandId) =>
        sourceBrandId == destinationBrandId;

    private static bool MatchesBrand(Guid? policyBrandId, Guid? warehouseBrandId) =>
        !policyBrandId.HasValue || policyBrandId == warehouseBrandId;
}
