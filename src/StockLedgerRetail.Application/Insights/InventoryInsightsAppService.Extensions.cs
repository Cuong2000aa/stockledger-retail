using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Application.Insights;

public partial class InventoryInsightsAppService
{
    public async Task<PagedResultDto<DeadStockInsightDto>> GetDeadStockPagedAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int daysWithoutOutbound = 60,
        decimal minOnHand = 1,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var fetchLimit = Math.Min(skip + take, 500);
        var items = await GetDeadStockAsync(
            warehouseId,
            brandId,
            regionCode,
            daysWithoutOutbound,
            minOnHand,
            fetchLimit,
            cancellationToken);

        var pageItems = items.Skip(skip).Take(take).ToList();
        var totalCount = items.Count < fetchLimit ? items.Count : skip + take + 1;
        return PagingNormalizer.Create(pageItems, totalCount, normalizedPage, normalizedPageSize);
    }

    public async Task<List<BrokenSizeRunInsightDto>> GetBrokenSizeRunsAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        int lookbackDays = 30,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var (resolvedWarehouseId, scopedWarehouseIds) = ResolveWarehouseScope(warehouseId);
        warehouseId = resolvedWarehouseId;

        var normalizedLookback = NormalizePositive(lookbackDays, 30);
        var normalizedMax = NormalizePositive(maxResults, 50, 200);
        var toDateUtc = DateTime.UtcNow;
        var fromDateUtc = toDateUtc.AddDays(-normalizedLookback);

        var facts = await _inventoryInsightReadRepository.GetFashionStockFactsAsync(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            fromDateUtc,
            toDateUtc,
            2000,
            scopedWarehouseIds,
            cancellationToken);

        var runs = FashionInsightAnalyzer.BuildBrokenSizeRuns(facts, normalizedMax);
        foreach (var run in runs)
        {
            run.Recommendation = new InsightRecommendationDto
            {
                ActionCode = InsightActionCodes.BrokenSizeRunConsolidate,
                ActionType = InsightActionTypes.Transfer,
                TitleKey = InsightActionCodes.BrokenSizeRunConsolidate,
                Priority = run.Severity == "critical" ? 82 : 62,
                Params = new Dictionary<string, string>
                {
                    ["productId"] = run.ProductId.ToString(),
                    ["productName"] = run.ProductName,
                    ["warehouseCode"] = run.WarehouseCode,
                    ["missingSizes"] = string.Join(",", run.MissingSizes)
                },
                Evidence = new Dictionary<string, string>
                {
                    ["sizesWithStock"] = run.SizesWithStock.ToString(),
                    ["missingSizes"] = run.SizesWithoutStock.ToString(),
                    ["totalOnHand"] = run.TotalOnHand.ToString("0.##")
                }
            };
        }

        return runs;
    }

    public async Task<List<SeasonClearanceInsightDto>> GetSeasonClearanceAsync(
        Guid? warehouseId = null,
        Guid? brandId = null,
        string? regionCode = null,
        string? currentSeason = null,
        int lookbackDays = 30,
        int daysWithoutOutbound = 60,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var scopedBrandId = _brandScopeContext.BrandId ?? brandId;
        var scopedRegionCode = _brandScopeContext.RegionCode ?? regionCode;
        var (resolvedWarehouseId, scopedWarehouseIds) = ResolveWarehouseScope(warehouseId);
        warehouseId = resolvedWarehouseId;

        var normalizedLookback = NormalizePositive(lookbackDays, 30);
        var normalizedDays = NormalizePositive(daysWithoutOutbound, 60);
        var normalizedMax = NormalizePositive(maxResults, 50, 200);
        var toDateUtc = DateTime.UtcNow;
        var fromDateUtc = toDateUtc.AddDays(-normalizedLookback);

        var facts = await _inventoryInsightReadRepository.GetFashionStockFactsAsync(
            warehouseId,
            scopedBrandId,
            scopedRegionCode,
            fromDateUtc,
            toDateUtc,
            2000,
            scopedWarehouseIds,
            cancellationToken);

        var items = FashionInsightAnalyzer.BuildSeasonClearance(
            facts,
            currentSeason,
            normalizedDays,
            normalizedMax);

        var policies = await _markdownPolicyRepository.GetActivePoliciesAsync(cancellationToken);
        foreach (var item in items)
        {
            var suggestion = _markdownPolicyEngine.Evaluate(
                new MarkdownEvaluationInput(
                    item.BrandId,
                    null,
                    WarehouseType.Store,
                    item.DaysWithoutOutbound,
                    item.QuantityOnHand,
                    item.InventoryValue.HasValue && item.QuantityOnHand > 0
                        ? item.InventoryValue / item.QuantityOnHand
                        : null,
                    null,
                    10m,
                    item.OutboundQuantity,
                    0m),
                policies);

            if (suggestion is not null)
            {
                item.SuggestedMarkdownPriceAfterVat = suggestion.SuggestedMarkdownPriceAfterVat;
                item.MarkdownDepthPercent = suggestion.MarkdownDepthPercent;
            }

            item.Recommendation = new InsightRecommendationDto
            {
                ActionCode = InsightActionCodes.SeasonClearanceMarkdown,
                ActionType = InsightActionTypes.Markdown,
                TitleKey = InsightActionCodes.SeasonClearanceMarkdown,
                Priority = item.Severity == "critical" ? 80 : 58,
                Params = new Dictionary<string, string>
                {
                    ["sku"] = item.Sku,
                    ["season"] = item.Season ?? string.Empty,
                    ["warehouseCode"] = item.WarehouseCode,
                    ["days"] = item.DaysWithoutOutbound.ToString()
                },
                Evidence = new Dictionary<string, string>
                {
                    ["daysIdle"] = item.DaysWithoutOutbound.ToString(),
                    ["quantityOnHand"] = item.QuantityOnHand.ToString("0.##"),
                    ["inventoryValue"] = (item.InventoryValue ?? 0).ToString("0.##")
                }
            };
        }

        return items;
    }

    public Task<MarkdownWhatIfResultDto> SimulateMarkdownWhatIfAsync(
        MarkdownWhatIfRequestDto input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_markdownWhatIfService.Simulate(input));

    public Task<InsightExplainResponseDto> ExplainAsync(
        InsightExplainRequestDto input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_insightExplainService.Explain(input));

    public async Task<BulkTransferFromInsightsResultDto> CreateBulkTransfersAsync(
        BulkTransferFromInsightsRequestDto input,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkTransferFromInsightsResultDto();
        if (input.Lines.Count == 0)
        {
            result.Errors.Add("No transfer lines were provided.");
            return result;
        }

        var groups = input.Lines
            .GroupBy(x => new { x.SourceWarehouseId, x.DestinationWarehouseId })
            .ToList();

        foreach (var group in groups)
        {
            try
            {
                var document = await _inventoryDocumentAppService.CreateTransferAsync(new CreateTransferDto
                {
                    SourceWarehouseId = group.Key.SourceWarehouseId,
                    DestinationWarehouseId = group.Key.DestinationWarehouseId,
                    Note = input.Note ?? "[INSIGHT] Bulk transfer",
                    Lines = group.Select(line => new CreateInventoryDocumentLineDto
                    {
                        ProductVariantId = line.ProductVariantId,
                        Quantity = line.Quantity,
                        Note = line.Sku
                    }).ToList()
                }, cancellationToken);

                result.Documents.Add(new BulkTransferDocumentResultDto
                {
                    DocumentId = document.Id,
                    DocumentNo = document.DocumentNo,
                    SourceWarehouseId = group.Key.SourceWarehouseId,
                    DestinationWarehouseId = group.Key.DestinationWarehouseId,
                    LineCount = group.Count()
                });
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add(ex.Message);
            }
        }

        return result;
    }
}
