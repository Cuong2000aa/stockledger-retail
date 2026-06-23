using Microsoft.Extensions.Logging;
using StockLedgerRetail.Domain.Inventory;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>
/// So sánh tổng QuantityDelta trên sổ cái với CurrentStock.QuantityOnHand.
/// </summary>
public class StockReconciliationService : IStockReconciliationService
{
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly ILogger<StockReconciliationService> _logger;

    public StockReconciliationService(
        IStockTransactionRepository stockTransactionRepository,
        ICurrentStockRepository currentStockRepository,
        ILogger<StockReconciliationService> logger)
    {
        _stockTransactionRepository = stockTransactionRepository;
        _currentStockRepository = currentStockRepository;
        _logger = logger;
    }

    public async Task<StockReconciliationResultDto> RunAsync(CancellationToken cancellationToken = default)
    {
        var ledger = await _stockTransactionRepository.GetAggregatedQuantitiesAsync(cancellationToken);
        var currentStocks = await _currentStockRepository.GetListAsync(cancellationToken: cancellationToken);

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
                "Stock reconciliation found {MismatchCount} mismatch(es) across {TotalPairs} variant/warehouse pairs.",
                mismatches.Count,
                allKeys.Count);
        }
        else
        {
            _logger.LogInformation(
                "Stock reconciliation OK — {TotalPairs} variant/warehouse pairs checked.",
                allKeys.Count);
        }

        return new StockReconciliationResultDto
        {
            CheckedAt = DateTime.UtcNow,
            TotalPairsChecked = allKeys.Count,
            MismatchCount = mismatches.Count,
            Mismatches = mismatches
                .OrderByDescending(x => Math.Abs(x.Variance))
                .Select(x => new StockReconciliationMismatchDto
                {
                    ProductVariantId = x.ProductVariantId,
                    WarehouseId = x.WarehouseId,
                    LedgerQuantity = x.LedgerQuantity,
                    CurrentStockQuantity = x.CurrentStockQuantity,
                    Variance = x.Variance
                })
                .ToList()
        };
    }
}
