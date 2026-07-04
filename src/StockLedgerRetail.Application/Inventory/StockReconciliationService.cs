using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Domain.Inventory;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>
/// So sánh tổng QuantityDelta trên nhật ký biến động tồn với CurrentStock.QuantityOnHand.
/// </summary>
public class StockReconciliationService : IStockReconciliationService
{
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IWarehouseScopeService _warehouseScopeService;
    private readonly StockReconciliationOptions _options;
    private readonly ILogger<StockReconciliationService> _logger;

    public StockReconciliationService(
        IStockTransactionRepository stockTransactionRepository,
        ICurrentStockRepository currentStockRepository,
        IWarehouseScopeService warehouseScopeService,
        IOptions<StockReconciliationOptions> options,
        ILogger<StockReconciliationService> logger)
    {
        _stockTransactionRepository = stockTransactionRepository;
        _currentStockRepository = currentStockRepository;
        _warehouseScopeService = warehouseScopeService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<StockReconciliationResultDto> RunAsync(CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(null);
        var runFull = ShouldRunFullReconciliation();

        List<StockLedgerAggregate> ledger;
        List<Domain.Entities.CurrentStock> currentStocks;

        if (!_options.IncrementalOnly || runFull)
        {
            ledger = await _stockTransactionRepository.GetAggregatedQuantitiesAsync(
                scope.WarehouseId,
                scope.ScopedWarehouseIds,
                cancellationToken);
            currentStocks = await _currentStockRepository.GetListAsync(
                scope.WarehouseId,
                scopedWarehouseIds: scope.ScopedWarehouseIds,
                cancellationToken: cancellationToken);
        }
        else
        {
            var sinceUtc = DateTime.UtcNow.AddHours(-Math.Max(1, _options.IncrementalLookbackHours));
            var activePairs = await _stockTransactionRepository.GetActivePairsSinceAsync(
                sinceUtc,
                scope.WarehouseId,
                scope.ScopedWarehouseIds,
                cancellationToken);

            if (activePairs.Count == 0)
            {
                _logger.LogInformation(
                    "Incremental stock reconciliation skipped — no stock activity since {SinceUtc}.",
                    sinceUtc);
                return new StockReconciliationResultDto
                {
                    CheckedAt = DateTime.UtcNow,
                    TotalPairsChecked = 0,
                    MismatchCount = 0
                };
            }

            var pairTuples = activePairs
                .Select(x => (x.ProductVariantId, x.WarehouseId))
                .ToList();

            ledger = await _stockTransactionRepository.GetAggregatedQuantitiesForPairsAsync(
                pairTuples,
                cancellationToken);

            var variantIds = activePairs.Select(x => x.ProductVariantId).Distinct().ToList();
            var warehouseIds = activePairs.Select(x => x.WarehouseId).Distinct().ToList();
            currentStocks = await _currentStockRepository.GetByVariantsAndWarehousesAsync(
                variantIds,
                warehouseIds,
                cancellationToken);
        }

        var ledgerMap = ledger.ToDictionary(
            x => (x.ProductVariantId, x.WarehouseId),
            x => x.LedgerQuantity);

        var currentMap = currentStocks.ToDictionary(
            x => (x.ProductVariantId, x.WarehouseId),
            x => x.QuantityOnHand);

        var allKeys = ledgerMap.Keys.Union(currentMap.Keys).ToHashSet();
        var mismatches = new List<StockReconciliationMismatch>();

        foreach (var key in allKeys)
        {
            ledgerMap.TryGetValue(key, out var ledgerQty);
            currentMap.TryGetValue(key, out var currentQty);

            if (ledgerQty != currentQty)
            {
                mismatches.Add(new StockReconciliationMismatch
                {
                    ProductVariantId = key.ProductVariantId,
                    WarehouseId = key.WarehouseId,
                    LedgerQuantity = ledgerQty,
                    CurrentStockQuantity = currentQty
                });
            }
        }

        if (mismatches.Count > 0)
        {
            _logger.LogWarning(
                "Stock reconciliation ({Mode}) found {MismatchCount} mismatch(es) across {TotalPairs} variant/warehouse pairs.",
                runFull ? "full" : "incremental",
                mismatches.Count,
                allKeys.Count);
        }
        else
        {
            _logger.LogInformation(
                "Stock reconciliation ({Mode}) OK — {TotalPairs} variant/warehouse pairs checked.",
                runFull ? "full" : "incremental",
                allKeys.Count);
        }

        return new StockReconciliationResultDto
        {
            CheckedAt = DateTime.UtcNow,
            TotalPairsChecked = allKeys.Count,
            MismatchCount = mismatches.Count,
            Mismatches = mismatches
                .Select(x => new StockReconciliationMismatchDto
                {
                    ProductVariantId = x.ProductVariantId,
                    WarehouseId = x.WarehouseId,
                    LedgerQuantity = x.LedgerQuantity,
                    CurrentStockQuantity = x.CurrentStockQuantity,
                    Variance = x.CurrentStockQuantity - x.LedgerQuantity
                })
                .ToList()
        };
    }

    private bool ShouldRunFullReconciliation()
    {
        if (!_options.IncrementalOnly)
        {
            return true;
        }

        return DateTime.UtcNow.Hour == Math.Clamp(_options.FullReconciliationHourUtc, 0, 23);
    }
}
