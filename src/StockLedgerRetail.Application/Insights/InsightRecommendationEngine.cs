using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Application.Insights;

public interface IInsightRecommendationEngine
{
    InsightRecommendationDto BuildDeadStock(DeadStockInsightDto insight, InsightRecommendationContext context);

    InsightRecommendationDto BuildSalesVelocity(SalesVelocityInsightDto insight, InsightRecommendationContext context);

    InsightRecommendationDto BuildTransfer(TransferSuggestionDto insight, InsightRecommendationContext context);

    InsightRecommendationDto BuildMarkdownCandidate(MarkdownCandidateInsightDto insight, InsightRecommendationContext context);

    InsightRecommendationDto BuildPromotionRisk(PromotionRiskInsightDto insight, InsightRecommendationContext context);

    InsightRecommendationDto BuildReorderRisk(ReorderRiskInsightDto insight, InsightRecommendationContext context);

    InsightRecommendationDto BuildTrendSummary(TrendSummaryInsightDto insight, InsightRecommendationContext context);
}

public class InsightRecommendationEngine : IInsightRecommendationEngine
{
    public InsightRecommendationDto BuildDeadStock(DeadStockInsightDto insight, InsightRecommendationContext context)
    {
        var (actionCode, parameters) = ResolveDeadStockAction(insight, context);
        var recommendation = new InsightRecommendationDto
        {
            ActionCode = actionCode,
            ActionType = MapActionType(actionCode),
            TitleKey = actionCode,
            Priority = CalculateDeadStockPriority(insight),
            Params = parameters,
            Evidence = BuildDeadStockEvidence(insight, context),
            Actions = BuildDeadStockActions(insight, actionCode, context)
        };

        return recommendation;
    }

    public InsightRecommendationDto BuildSalesVelocity(SalesVelocityInsightDto insight, InsightRecommendationContext context)
    {
        var (actionCode, parameters) = ResolveSalesVelocityAction(insight);
        var recommendation = new InsightRecommendationDto
        {
            ActionCode = actionCode,
            ActionType = MapActionType(actionCode),
            TitleKey = actionCode,
            Priority = CalculateVelocityPriority(insight),
            Params = parameters,
            Evidence = BuildVelocityEvidence(insight, context),
            Actions = BuildVelocityActions(insight, actionCode, context)
        };

        return recommendation;
    }

    public InsightRecommendationDto BuildTransfer(TransferSuggestionDto insight, InsightRecommendationContext context)
    {
        var (actionCode, parameters) = (
            InsightActionCodes.TransferExecute,
            new Dictionary<string, string>
            {
                ["sku"] = insight.Sku,
                ["sourceWarehouseCode"] = insight.SourceWarehouseCode,
                ["destinationWarehouseCode"] = insight.DestinationWarehouseCode,
                ["quantity"] = insight.SuggestedQuantity.ToString("0.##")
            });

        return new InsightRecommendationDto
        {
            ActionCode = actionCode,
            ActionType = InsightActionTypes.Transfer,
            TitleKey = actionCode,
            Priority = CalculateTransferPriority(insight),
            Params = parameters,
            Evidence = BuildTransferEvidence(insight),
            Actions = BuildTransferActions(insight)
        };
    }

    public InsightRecommendationDto BuildMarkdownCandidate(MarkdownCandidateInsightDto insight, InsightRecommendationContext context)
    {
        var parameters = new Dictionary<string, string>
        {
            ["sku"] = insight.Sku,
            ["warehouseCode"] = insight.WarehouseCode,
            ["days"] = insight.DaysWithoutOutbound.ToString(),
            ["markdownDepth"] = insight.MarkdownDepthPercent?.ToString("0.##") ?? "0"
        };

        var actionCode = insight.Severity == "critical"
            ? InsightActionCodes.MarkdownCandidateExecute
            : InsightActionCodes.MarkdownCandidateReview;

        return new InsightRecommendationDto
        {
            ActionCode = actionCode,
            ActionType = InsightActionTypes.Markdown,
            TitleKey = actionCode,
            Priority = insight.Severity == "critical" ? 88 : 68,
            Params = parameters,
            Evidence = new Dictionary<string, string>
            {
                ["sku"] = insight.Sku,
                ["warehouseCode"] = insight.WarehouseCode,
                ["daysIdle"] = insight.DaysWithoutOutbound.ToString(),
                ["inventoryValue"] = (insight.EstimatedInventoryValue ?? 0).ToString("0.##")
            },
            Actions = BuildMarkdownCandidateActions(insight, actionCode)
        };
    }

    private static List<InsightRecommendationCtaDto> BuildMarkdownCandidateActions(
        MarkdownCandidateInsightDto insight,
        string actionCode)
    {
        var actions = new List<InsightRecommendationCtaDto>
        {
            NavigateCta(
                "view_history",
                "view_history",
                "/stock-transactions",
                new Dictionary<string, string>
                {
                    ["search"] = insight.Sku,
                    ["warehouseId"] = insight.WarehouseId.ToString()
                },
                isPrimary: actionCode == InsightActionCodes.MarkdownCandidateReview
                    && !insight.SuggestedMarkdownPriceBeforeVat.HasValue)
        };

        if (insight.SuggestedMarkdownPriceBeforeVat.HasValue)
        {
            AddMarkdownPriceCtas(
                actions,
                insight.ProductVariantId,
                insight.Sku,
                insight.SuggestedMarkdownPriceBeforeVat,
                insight.SuggestedMarkdownPriceAfterVat,
                insight.MarkdownDepthPercent,
                isPrimary: actionCode == InsightActionCodes.MarkdownCandidateExecute);
        }

        actions.Add(NavigateCta(
            "review_sku",
            "review_sku",
            "/product-variants",
            new Dictionary<string, string>
            {
                ["search"] = insight.Sku,
                ["variantId"] = insight.ProductVariantId.ToString()
            },
            isPrimary: actionCode == InsightActionCodes.MarkdownCandidateExecute
                && !insight.SuggestedMarkdownPriceBeforeVat.HasValue));

        return actions;
    }

    public InsightRecommendationDto BuildPromotionRisk(PromotionRiskInsightDto insight, InsightRecommendationContext context)
    {
        var actionCode = insight.Severity == "critical"
            ? InsightActionCodes.PromotionRiskTightStock
            : InsightActionCodes.PromotionRiskReview;

        return new InsightRecommendationDto
        {
            ActionCode = actionCode,
            ActionType = InsightActionTypes.Promote,
            TitleKey = actionCode,
            Priority = insight.Severity == "critical" ? 85 : 60,
            Params = new Dictionary<string, string>
            {
                ["sku"] = insight.Sku,
                ["warehouseCode"] = insight.WarehouseCode,
                ["coverDays"] = insight.EstimatedDaysOfCover?.ToString("0.##") ?? "0"
            },
            Evidence = new Dictionary<string, string>
            {
                ["sku"] = insight.Sku,
                ["warehouseCode"] = insight.WarehouseCode,
                ["discountPercent"] = insight.PromotionDiscountPercent?.ToString("0.##") ?? "0",
                ["marginRate"] = insight.MarginRateAfterPromotion?.ToString("0.##") ?? "0"
            },
            Actions = new List<InsightRecommendationCtaDto>
            {
                NavigateCta(
                    "view_stock",
                    "view_stock",
                    "/current-stocks",
                    new Dictionary<string, string>
                    {
                        ["search"] = insight.Sku,
                        ["warehouseId"] = insight.WarehouseId.ToString()
                    },
                    isPrimary: actionCode == InsightActionCodes.PromotionRiskTightStock),
                NavigateCta(
                    "review_sku",
                    "review_sku",
                    "/product-variants",
                    new Dictionary<string, string>
                    {
                        ["search"] = insight.Sku
                    })
            }
        };
    }

    public InsightRecommendationDto BuildReorderRisk(ReorderRiskInsightDto insight, InsightRecommendationContext context)
    {
        var actionCode = insight.Severity == "critical"
            ? InsightActionCodes.ReorderRiskUrgent
            : InsightActionCodes.ReorderRiskPlan;

        var orderedQuantity = Math.Max(1m, Math.Ceiling(insight.SuggestedReorderQuantity ?? 1m));
        return new InsightRecommendationDto
        {
            ActionCode = actionCode,
            ActionType = InsightActionTypes.Replenish,
            TitleKey = actionCode,
            Priority = insight.Severity == "critical" ? 92 : 70,
            Params = new Dictionary<string, string>
            {
                ["sku"] = insight.Sku,
                ["warehouseCode"] = insight.WarehouseCode,
                ["suggestedQty"] = orderedQuantity.ToString("0.##")
            },
            Evidence = new Dictionary<string, string>
            {
                ["sku"] = insight.Sku,
                ["warehouseCode"] = insight.WarehouseCode,
                ["onOrder"] = insight.QuantityOnOrder.ToString("0.##"),
                ["inReceiving"] = insight.QuantityInReceiving.ToString("0.##")
            },
            Actions = new List<InsightRecommendationCtaDto>
            {
                NavigateCta(
                    "draft_po",
                    "draft_po",
                    "/purchase-orders/new",
                    new Dictionary<string, string>
                    {
                        ["productVariantId"] = insight.ProductVariantId.ToString(),
                        ["warehouseId"] = insight.WarehouseId.ToString(),
                        ["orderedQuantity"] = orderedQuantity.ToString("0.##"),
                        ["note"] = $"[INSIGHT] Reorder {insight.Sku} at {insight.WarehouseCode}"
                    },
                    isPrimary: true),
                NavigateCta(
                    "view_stock",
                    "view_stock",
                    "/current-stocks",
                    new Dictionary<string, string>
                    {
                        ["search"] = insight.Sku,
                        ["warehouseId"] = insight.WarehouseId.ToString()
                    })
            }
        };
    }

    public InsightRecommendationDto BuildTrendSummary(TrendSummaryInsightDto insight, InsightRecommendationContext context)
    {
        return new InsightRecommendationDto
        {
            ActionCode = InsightActionCodes.TrendReview,
            ActionType = InsightActionTypes.Trend,
            TitleKey = InsightActionCodes.TrendReview,
            Priority = insight.Severity == "warning" ? 58 : 35,
            Params = new Dictionary<string, string>
            {
                ["sku"] = insight.Sku,
                ["warehouseCode"] = insight.WarehouseCode
            },
            Evidence = new Dictionary<string, string>
            {
                ["inventoryDelta"] = insight.InventoryValueDelta.ToString("0.##"),
                ["outboundTrend"] = insight.OutboundTrendPercent.ToString("0.##"),
                ["priceTrend"] = insight.PriceTrendPercent?.ToString("0.##") ?? "0"
            },
            Actions = new List<InsightRecommendationCtaDto>
            {
                NavigateCta(
                    "view_history",
                    "view_history",
                    "/stock-transactions",
                    new Dictionary<string, string>
                    {
                        ["search"] = insight.Sku,
                        ["warehouseId"] = insight.WarehouseId.ToString()
                    },
                    isPrimary: true),
                NavigateCta(
                    "open_reports",
                    "open_reports",
                    "/reports",
                    new Dictionary<string, string>
                    {
                        ["search"] = insight.Sku
                    })
            }
        };
    }

    private static (string ActionCode, Dictionary<string, string> Parameters) ResolveDeadStockAction(
        DeadStockInsightDto insight,
        InsightRecommendationContext context)
    {
        var parameters = new Dictionary<string, string>
        {
            ["sku"] = insight.Sku,
            ["warehouseCode"] = insight.WarehouseCode,
            ["days"] = insight.DaysWithoutOutbound.ToString()
        };

        var sourceWarehouse = context.Warehouses.FirstOrDefault(x => x.Id == insight.WarehouseId);
        var clearanceDestination = sourceWarehouse is null
            ? null
            : InsightTransferRules.FindClearanceDestination(
                context.Warehouses,
                sourceWarehouse,
                insight.BrandId,
                context.TransferPolicies);

        if (clearanceDestination is not null)
        {
            parameters["destinationWarehouseCode"] = clearanceDestination.Code;
        }

        if (sourceWarehouse is not null && InsightTransferRules.IsClearanceWarehouse(sourceWarehouse.Type))
        {
            if (insight.EstimatedCostValue is > 1000)
            {
                parameters["costValue"] = insight.EstimatedCostValue.Value.ToString("0.##");
            }

            return (InsightActionCodes.DeadStockCriticalMarkdown, parameters);
        }

        if (insight.Severity == "critical" || insight.DaysWithoutOutbound >= 120)
        {
            if (insight.EstimatedCostValue is > 1000)
            {
                parameters["costValue"] = insight.EstimatedCostValue.Value.ToString("0.##");
            }

            return clearanceDestination is not null
                ? (InsightActionCodes.DeadStockMarkdownOrTransfer, parameters)
                : (InsightActionCodes.DeadStockCriticalMarkdown, parameters);
        }

        if (insight.DaysWithoutOutbound >= 60)
        {
            return clearanceDestination is not null
                ? (InsightActionCodes.DeadStockMarkdownOrTransfer, parameters)
                : (InsightActionCodes.DeadStockReview, parameters);
        }

        return (InsightActionCodes.DeadStockReview, parameters);
    }

    private static (string ActionCode, Dictionary<string, string> Parameters) ResolveSalesVelocityAction(
        SalesVelocityInsightDto insight)
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

    private static Dictionary<string, string> BuildDeadStockEvidence(
        DeadStockInsightDto insight,
        InsightRecommendationContext context)
    {
        var evidence = new Dictionary<string, string>
        {
            ["sku"] = insight.Sku,
            ["warehouseCode"] = insight.WarehouseCode,
            ["daysIdle"] = insight.DaysWithoutOutbound.ToString(),
            ["onHand"] = insight.QuantityOnHand.ToString("0.##"),
            ["costValue"] = (insight.EstimatedCostValue ?? 0).ToString("0.##")
        };

        var sourceWarehouse = context.Warehouses.FirstOrDefault(x => x.Id == insight.WarehouseId);
        if (sourceWarehouse is not null)
        {
            evidence["warehouseType"] = sourceWarehouse.Type.ToString();
        }

        var clearance = sourceWarehouse is null
            ? null
            : InsightTransferRules.FindClearanceDestination(
                context.Warehouses,
                sourceWarehouse,
                insight.BrandId,
                context.TransferPolicies);

        if (clearance is null
            && actionNeedsTransfer(insight)
            && sourceWarehouse is not null
            && !InsightTransferRules.IsClearanceWarehouse(sourceWarehouse.Type))
        {
            evidence["transferBlocked"] = "no_clearance_destination";
        }

        return evidence;
    }

    private static bool actionNeedsTransfer(DeadStockInsightDto insight) =>
        insight.DaysWithoutOutbound >= 60;

    private static Dictionary<string, string> BuildVelocityEvidence(
        SalesVelocityInsightDto insight,
        InsightRecommendationContext context)
    {
        var evidence = new Dictionary<string, string>
        {
            ["sku"] = insight.Sku,
            ["warehouseCode"] = insight.WarehouseCode,
            ["onHand"] = insight.QuantityOnHand.ToString("0.##"),
            ["outboundQty"] = insight.OutboundQuantity.ToString("0.##"),
            ["avgDaily"] = insight.AverageDailyOutbound.ToString("0.##")
        };

        if (insight.EstimatedDaysOfCover.HasValue)
        {
            evidence["coverDays"] = insight.EstimatedDaysOfCover.Value.ToString("0.##");
        }

        var replenishKey = InsightRecommendationContext.BuildReplenishKey(
            insight.ProductVariantId,
            insight.WarehouseId);
        if (!context.ReplenishTransferByDestinationKey.ContainsKey(replenishKey)
            && insight.Severity is "critical" or "warning")
        {
            evidence["transferBlocked"] = "no_internal_source";
        }

        return evidence;
    }

    private static List<InsightRecommendationCtaDto> BuildDeadStockActions(
        DeadStockInsightDto insight,
        string actionCode,
        InsightRecommendationContext context)
    {
        var actions = new List<InsightRecommendationCtaDto>
        {
            NavigateCta(
                "view_stock",
                "view_stock",
                "/current-stocks",
                new Dictionary<string, string>
                {
                    ["search"] = insight.Sku,
                    ["warehouseId"] = insight.WarehouseId.ToString()
                },
                isPrimary: actionCode == InsightActionCodes.DeadStockReview)
        };

        var sourceWarehouse = context.Warehouses.FirstOrDefault(x => x.Id == insight.WarehouseId);
        var clearanceDestination = sourceWarehouse is null
            ? null
            : InsightTransferRules.FindClearanceDestination(
                context.Warehouses,
                sourceWarehouse,
                insight.BrandId,
                context.TransferPolicies);

        if (actionCode is InsightActionCodes.DeadStockCriticalMarkdown
            or InsightActionCodes.DeadStockMarkdownOrTransfer)
        {
            if (clearanceDestination is not null)
            {
                actions.Add(NavigateCta(
                    "draft_transfer",
                    "draft_transfer",
                    "/inventory-documents/new/transfer",
                    new Dictionary<string, string>
                    {
                        ["productVariantId"] = insight.ProductVariantId.ToString(),
                        ["sourceWarehouseId"] = insight.WarehouseId.ToString(),
                        ["destinationWarehouseId"] = clearanceDestination.Id.ToString(),
                        ["quantity"] = insight.QuantityOnHand.ToString("0.##"),
                        ["note"] = $"[INSIGHT] Move {insight.Sku} to clearance {clearanceDestination.Code}"
                    },
                    isPrimary: true));
            }
            else if (actionCode == InsightActionCodes.DeadStockCriticalMarkdown)
            {
                actions.Add(NavigateCta(
                    "draft_stock_out",
                    "draft_stock_out",
                    "/inventory-documents/new/stock-out",
                    new Dictionary<string, string>
                    {
                        ["productVariantId"] = insight.ProductVariantId.ToString(),
                        ["warehouseId"] = insight.WarehouseId.ToString(),
                        ["quantity"] = insight.QuantityOnHand.ToString("0.##"),
                        ["note"] = $"[INSIGHT] Markdown clearance for {insight.Sku}"
                    },
                    isPrimary: true));
            }
        }

        if (actionCode == InsightActionCodes.DeadStockCriticalMarkdown
            && clearanceDestination is not null)
        {
            actions.Add(NavigateCta(
                "draft_stock_out",
                "draft_stock_out",
                "/inventory-documents/new/stock-out",
                new Dictionary<string, string>
                {
                    ["productVariantId"] = insight.ProductVariantId.ToString(),
                    ["warehouseId"] = insight.WarehouseId.ToString(),
                    ["quantity"] = insight.QuantityOnHand.ToString("0.##"),
                    ["note"] = $"[INSIGHT] Markdown clearance for {insight.Sku}"
                }));
        }

        if (insight.SuggestedMarkdownPriceBeforeVat.HasValue
            && actionCode is InsightActionCodes.DeadStockCriticalMarkdown
                or InsightActionCodes.DeadStockMarkdownOrTransfer
                or InsightActionCodes.DeadStockReview)
        {
            AddMarkdownPriceCtas(
                actions,
                insight.ProductVariantId,
                insight.Sku,
                insight.SuggestedMarkdownPriceBeforeVat,
                insight.SuggestedMarkdownPriceAfterVat,
                insight.MarkdownDepthPercent,
                isPrimary: true);
        }

        return actions;
    }

    private static List<InsightRecommendationCtaDto> BuildVelocityActions(
        SalesVelocityInsightDto insight,
        string actionCode,
        InsightRecommendationContext context)
    {
        var actions = new List<InsightRecommendationCtaDto>
        {
            NavigateCta(
                "view_stock",
                "view_stock",
                "/current-stocks",
                new Dictionary<string, string>
                {
                    ["search"] = insight.Sku,
                    ["warehouseId"] = insight.WarehouseId.ToString()
                },
                isPrimary: actionCode == InsightActionCodes.VelocityMonitor)
        };

        if (actionCode is InsightActionCodes.VelocityReplenishUrgent
            or InsightActionCodes.VelocityReplenishPlan)
        {
            var suggestedQty = insight.AverageDailyOutbound > 0
                ? Math.Ceiling(insight.AverageDailyOutbound * 14)
                : 1m;

            var replenishKey = InsightRecommendationContext.BuildReplenishKey(
                insight.ProductVariantId,
                insight.WarehouseId);
            context.ReplenishTransferByDestinationKey.TryGetValue(replenishKey, out var internalTransfer);

            if (internalTransfer is not null)
            {
                actions.Add(NavigateCta(
                    "draft_transfer",
                    "draft_transfer",
                    "/inventory-documents/new/transfer",
                    new Dictionary<string, string>
                    {
                        ["productVariantId"] = insight.ProductVariantId.ToString(),
                        ["sourceWarehouseId"] = internalTransfer.SourceWarehouseId.ToString(),
                        ["destinationWarehouseId"] = insight.WarehouseId.ToString(),
                        ["quantity"] = Math.Min(internalTransfer.SuggestedQuantity, suggestedQty).ToString("0.##"),
                        ["note"] = $"[INSIGHT] Replenish {insight.Sku} from {internalTransfer.SourceWarehouseCode}"
                    },
                    isPrimary: true));

                actions.Add(NavigateCta(
                    "draft_po",
                    "draft_po",
                    "/purchase-orders/new",
                    new Dictionary<string, string>
                    {
                        ["productVariantId"] = insight.ProductVariantId.ToString(),
                        ["warehouseId"] = insight.WarehouseId.ToString(),
                        ["orderedQuantity"] = suggestedQty.ToString("0.##"),
                        ["note"] = $"[INSIGHT] PO fallback for {insight.Sku} at {insight.WarehouseCode}"
                    }));
            }
            else
            {
                actions.Add(NavigateCta(
                    "draft_po",
                    "draft_po",
                    "/purchase-orders/new",
                    new Dictionary<string, string>
                    {
                        ["productVariantId"] = insight.ProductVariantId.ToString(),
                        ["warehouseId"] = insight.WarehouseId.ToString(),
                        ["orderedQuantity"] = suggestedQty.ToString("0.##"),
                        ["note"] = $"[INSIGHT] Replenish {insight.Sku} at {insight.WarehouseCode}"
                    },
                    isPrimary: true));
            }
        }

        if (actionCode == InsightActionCodes.VelocityNoDemandReview)
        {
            actions.Add(NavigateCta(
                "review_dead_stock",
                "review_dead_stock",
                "/insights",
                new Dictionary<string, string> { ["tab"] = "deadStock" },
                isPrimary: true));
        }

        return actions;
    }

    private static List<InsightRecommendationCtaDto> BuildTransferActions(TransferSuggestionDto insight) =>
    [
        new InsightRecommendationCtaDto
        {
            Id = "create_transfer",
            LabelKey = "create_transfer",
            Kind = InsightCtaKinds.Api,
            ApiOperation = InsightApiOperations.CreateTransfer,
            IsPrimary = true,
            Payload = new Dictionary<string, string>
            {
                ["productVariantId"] = insight.ProductVariantId.ToString(),
                ["sourceWarehouseId"] = insight.SourceWarehouseId.ToString(),
                ["destinationWarehouseId"] = insight.DestinationWarehouseId.ToString(),
                ["quantity"] = insight.SuggestedQuantity.ToString("0.##"),
                ["sku"] = insight.Sku,
                ["sourceWarehouseCode"] = insight.SourceWarehouseCode,
                ["destinationWarehouseCode"] = insight.DestinationWarehouseCode
            }
        },
        NavigateCta(
            "preview_transfer",
            "preview_transfer",
            "/inventory-documents/new/transfer",
            new Dictionary<string, string>
            {
                ["productVariantId"] = insight.ProductVariantId.ToString(),
                ["sourceWarehouseId"] = insight.SourceWarehouseId.ToString(),
                ["destinationWarehouseId"] = insight.DestinationWarehouseId.ToString(),
                ["quantity"] = insight.SuggestedQuantity.ToString("0.##"),
                ["note"] = $"[INSIGHT] {insight.Sku}: {insight.SourceWarehouseCode} → {insight.DestinationWarehouseCode}"
            })
    ];

    private static string MapActionType(string actionCode) =>
        actionCode switch
        {
            InsightActionCodes.DeadStockCriticalMarkdown or InsightActionCodes.DeadStockMarkdownOrTransfer =>
                InsightActionTypes.Markdown,
            InsightActionCodes.DeadStockReview or InsightActionCodes.VelocityNoDemandReview =>
                InsightActionTypes.Review,
            InsightActionCodes.VelocityReplenishUrgent or InsightActionCodes.VelocityReplenishPlan =>
                InsightActionTypes.Replenish,
            InsightActionCodes.VelocityMonitor => InsightActionTypes.Monitor,
            InsightActionCodes.TransferExecute => InsightActionTypes.Transfer,
            _ => InsightActionTypes.Review
        };

    private static int CalculateDeadStockPriority(DeadStockInsightDto insight)
    {
        var priority = 40 + Math.Min(insight.DaysWithoutOutbound / 2, 40);
        if (insight.Severity == "critical")
        {
            priority += 15;
        }

        if (insight.EstimatedCostValue is > 10_000_000)
        {
            priority += 10;
        }
        else if (insight.EstimatedCostValue is > 1_000_000)
        {
            priority += 5;
        }

        return Math.Min(priority, 100);
    }

    private static int CalculateVelocityPriority(SalesVelocityInsightDto insight)
    {
        if (insight.AverageDailyOutbound <= 0 && insight.QuantityOnHand > 0)
        {
            return 55;
        }

        if (insight.Severity == "critical")
        {
            return 90;
        }

        if (insight.Severity == "warning")
        {
            return 70;
        }

        return 30;
    }

    private static int CalculateTransferPriority(TransferSuggestionDto insight)
    {
        var priority = 60;
        if (insight.Severity == "critical")
        {
            priority += 25;
        }
        else
        {
            priority += 10;
        }

        if (insight.DestinationDaysOfCover is < 7)
        {
            priority += 10;
        }

        return Math.Min(priority, 100);
    }

    private static Dictionary<string, string> BuildTransferEvidence(TransferSuggestionDto insight) =>
        new()
        {
            ["sku"] = insight.Sku,
            ["sourceWarehouseCode"] = insight.SourceWarehouseCode,
            ["destinationWarehouseCode"] = insight.DestinationWarehouseCode,
            ["quantity"] = insight.SuggestedQuantity.ToString("0.##"),
            ["destCoverDays"] = insight.DestinationDaysOfCover?.ToString("0.##") ?? "0"
        };

    private static InsightRecommendationCtaDto NavigateCta(
        string id,
        string labelKey,
        string route,
        Dictionary<string, string> payload,
        bool isPrimary = false) =>
        new()
        {
            Id = id,
            LabelKey = labelKey,
            Kind = InsightCtaKinds.Navigate,
            Route = route,
            IsPrimary = isPrimary,
            Payload = payload
        };

    private static void AddMarkdownPriceCtas(
        ICollection<InsightRecommendationCtaDto> actions,
        Guid productVariantId,
        string sku,
        decimal? beforeVat,
        decimal? afterVat,
        decimal? depthPercent,
        bool isPrimary)
    {
        if (!beforeVat.HasValue)
        {
            return;
        }

        var payload = new Dictionary<string, string>
        {
            ["search"] = sku,
            ["variantId"] = productVariantId.ToString(),
            ["markdownBeforeVat"] = beforeVat.Value.ToString("0.##")
        };

        if (afterVat.HasValue)
        {
            payload["markdownAfterVat"] = afterVat.Value.ToString("0.##");
        }

        if (depthPercent.HasValue)
        {
            payload["markdownPercent"] = depthPercent.Value.ToString("0.##");
        }

        actions.Add(NavigateCta(
            "apply_markdown",
            "apply_markdown",
            "/product-variants",
            payload,
            isPrimary));
    }
}
