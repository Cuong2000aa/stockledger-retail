import { apiClient, createTransfer } from "@/lib/api";
import type {
  DeadStockInsight,
  InsightsExecutiveSummary,
  MarkdownCandidateInsight,
  PromotionRiskInsight,
  ReorderRiskInsight,
  SalesVelocityInsight,
  TrendSummaryInsight,
  TransferSuggestion,
} from "./types";
import { createTransferPayloadFromCta } from "./recommendation-utils";

type InsightScope = {
  warehouseId?: string;
  brandId?: string;
};

const scopeParams = ({ warehouseId, brandId }: InsightScope) => ({
  ...(warehouseId ? { warehouseId } : {}),
  ...(brandId ? { brandId } : {}),
});

export const fetchDeadStockInsights = (
  scope: InsightScope = {},
  daysWithoutOutbound = 60,
  minOnHand = 1,
  maxResults = 20
) =>
  apiClient
    .get<DeadStockInsight[]>("/api/inventory-insights/dead-stock", {
      params: { ...scopeParams(scope), daysWithoutOutbound, minOnHand, maxResults },
    })
    .then((r) => r.data);

export const fetchSalesVelocityInsights = (
  scope: InsightScope = {},
  lookbackDays = 30,
  maxResults = 20
) =>
  apiClient
    .get<SalesVelocityInsight[]>("/api/inventory-insights/sales-velocity", {
      params: { ...scopeParams(scope), lookbackDays, maxResults },
    })
    .then((r) => r.data);

export const fetchTransferSuggestions = (
  sourceWarehouseId?: string,
  destinationWarehouseId?: string,
  lookbackDays = 30,
  targetCoverDays = 14,
  reserveCoverDays = 7,
  maxResults = 20,
  brandId?: string
) =>
  apiClient
    .get<TransferSuggestion[]>("/api/inventory-insights/transfer-suggestions", {
      params: {
        sourceWarehouseId,
        destinationWarehouseId,
        ...(brandId ? { brandId } : {}),
        lookbackDays,
        targetCoverDays,
        reserveCoverDays,
        maxResults,
      },
    })
    .then((r) => r.data);

export const fetchMarkdownCandidates = (
  scope: InsightScope = {},
  daysWithoutOutbound = 60,
  minOnHand = 1,
  maxResults = 20
) =>
  apiClient
    .get<MarkdownCandidateInsight[]>("/api/inventory-insights/markdown-candidates", {
      params: { ...scopeParams(scope), daysWithoutOutbound, minOnHand, maxResults },
    })
    .then((r) => r.data);

export const fetchPromotionRiskInsights = (
  scope: InsightScope = {},
  lookbackDays = 30,
  maxResults = 20
) =>
  apiClient
    .get<PromotionRiskInsight[]>("/api/inventory-insights/promotion-risk", {
      params: { ...scopeParams(scope), lookbackDays, maxResults },
    })
    .then((r) => r.data);

export const fetchReorderRiskInsights = (
  scope: InsightScope = {},
  lookbackDays = 30,
  maxResults = 20
) =>
  apiClient
    .get<ReorderRiskInsight[]>("/api/inventory-insights/reorder-risk", {
      params: { ...scopeParams(scope), lookbackDays, maxResults },
    })
    .then((r) => r.data);

export const fetchTrendSummaryInsights = (
  scope: InsightScope = {},
  lookbackDays = 30,
  maxResults = 20
) =>
  apiClient
    .get<TrendSummaryInsight[]>("/api/inventory-insights/trend-summary", {
      params: { ...scopeParams(scope), lookbackDays, maxResults },
    })
    .then((r) => r.data);

export const fetchInsightsExecutiveSummary = (
  scope: InsightScope = {},
  lookbackDays = 30,
  daysWithoutOutbound = 60
) =>
  apiClient
    .get<InsightsExecutiveSummary>("/api/inventory-insights/executive-summary", {
      params: { ...scopeParams(scope), lookbackDays, daysWithoutOutbound },
    })
    .then((r) => r.data);

export const createTransferFromSuggestion = (item: TransferSuggestion) =>
  createTransfer({
    sourceWarehouseId: item.sourceWarehouseId,
    destinationWarehouseId: item.destinationWarehouseId,
    documentDate: new Date().toISOString(),
    note: `[INSIGHT] ${item.sku}: ${item.sourceWarehouseCode} → ${item.destinationWarehouseCode}`,
    lines: [
      {
        productVariantId: item.productVariantId,
        quantity: item.suggestedQuantity,
      },
    ],
  });

export const createTransferFromCtaPayload = (payload: Record<string, string>) => {
  const item = createTransferPayloadFromCta(payload);
  return createTransfer({
    sourceWarehouseId: item.sourceWarehouseId,
    destinationWarehouseId: item.destinationWarehouseId,
    documentDate: new Date().toISOString(),
    note: `[INSIGHT] ${item.sku}: ${item.sourceWarehouseCode} → ${item.destinationWarehouseCode}`,
    lines: [
      {
        productVariantId: item.productVariantId,
        quantity: item.suggestedQuantity,
      },
    ],
  });
};
