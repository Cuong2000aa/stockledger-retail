namespace StockLedgerRetail.Application.Insights;

using StockLedgerRetail.Insights;

public static class InsightRecommendationBuilder
{
    public static (string ActionCode, Dictionary<string, string> Params) ForDeadStock(DeadStockInsightDto insight)
    {
        var parameters = new Dictionary<string, string>
        {
            ["sku"] = insight.Sku,
            ["warehouseCode"] = insight.WarehouseCode,
            ["days"] = insight.DaysWithoutOutbound.ToString()
        };

        if (insight.Severity == "critical" || insight.DaysWithoutOutbound >= 120)
        {
            if (insight.EstimatedCostValue is > 1000)
            {
                parameters["costValue"] = insight.EstimatedCostValue.Value.ToString("0.##");
            }

            return (InsightActionCodes.DeadStockCriticalMarkdown, parameters);
        }

        if (insight.DaysWithoutOutbound >= 60)
        {
            return (InsightActionCodes.DeadStockMarkdownOrTransfer, parameters);
        }

        return (InsightActionCodes.DeadStockReview, parameters);
    }

    public static (string ActionCode, Dictionary<string, string> Params) ForSalesVelocity(SalesVelocityInsightDto insight)
    {
        var parameters = new Dictionary<string, string>
        {
            ["sku"] = insight.Sku,
            ["warehouseCode"] = insight.WarehouseCode
        };

        if (insight.AverageDailyOutbound <= 0 && insight.QuantityOnHand > 0)
        {
            parameters["onHand"] = insight.QuantityOnHand.ToString("0.##");
            return (InsightActionCodes.VelocityNoDemandReview, parameters);
        }

        if (insight.EstimatedDaysOfCover.HasValue)
        {
            parameters["coverDays"] = insight.EstimatedDaysOfCover.Value.ToString("0.##");
        }

        if (insight.Severity == "critical")
        {
            return (InsightActionCodes.VelocityReplenishUrgent, parameters);
        }

        if (insight.Severity == "warning")
        {
            return (InsightActionCodes.VelocityReplenishPlan, parameters);
        }

        return (InsightActionCodes.VelocityMonitor, parameters);
    }

    public static (string ActionCode, Dictionary<string, string> Params) ForTransfer(TransferSuggestionDto insight)
    {
        return (InsightActionCodes.TransferExecute, new Dictionary<string, string>
        {
            ["sku"] = insight.Sku,
            ["sourceWarehouseCode"] = insight.SourceWarehouseCode,
            ["destinationWarehouseCode"] = insight.DestinationWarehouseCode,
            ["quantity"] = insight.SuggestedQuantity.ToString("0.##")
        });
    }
}
