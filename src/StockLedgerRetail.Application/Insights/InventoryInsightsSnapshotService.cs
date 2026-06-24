using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Insights;

public interface IInventoryInsightsSnapshotService
{
    Task RefreshAllScopesAsync(CancellationToken cancellationToken = default);
}

public class InventoryInsightsSnapshotService : IInventoryInsightsSnapshotService
{
    private readonly IInventoryInsightsAppService _inventoryInsightsAppService;
    private readonly IBrandRepository _brandRepository;
    private readonly ILogger<InventoryInsightsSnapshotService> _logger;

    public InventoryInsightsSnapshotService(
        IInventoryInsightsAppService inventoryInsightsAppService,
        IBrandRepository brandRepository,
        ILogger<InventoryInsightsSnapshotService> logger)
    {
        _inventoryInsightsAppService = inventoryInsightsAppService;
        _brandRepository = brandRepository;
        _logger = logger;
    }

    public async Task RefreshAllScopesAsync(CancellationToken cancellationToken = default)
    {
        await RefreshScopeAsync(null, null, cancellationToken);

        var brands = await _brandRepository.GetListAsync(cancellationToken);
        foreach (var brand in brands)
        {
            await RefreshScopeAsync(brand.Id, null, cancellationToken);
        }

        _logger.LogInformation("Insight snapshots refreshed for global scope and {BrandCount} brands.", brands.Count);
    }

    private async Task RefreshScopeAsync(Guid? brandId, string? regionCode, CancellationToken cancellationToken)
    {
        await _inventoryInsightsAppService.GetDeadStockAsync(
            null,
            brandId,
            regionCode,
            60,
            1,
            50,
            cancellationToken,
            forceRefresh: true);

        await _inventoryInsightsAppService.GetSalesVelocityAsync(
            null,
            brandId,
            regionCode,
            30,
            100,
            cancellationToken,
            forceRefresh: true);

        await _inventoryInsightsAppService.GetTransferSuggestionsAsync(
            null,
            null,
            brandId,
            regionCode,
            30,
            14,
            7,
            20,
            cancellationToken,
            forceRefresh: true);
    }
}
