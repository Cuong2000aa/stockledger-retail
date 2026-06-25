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

export const fetchDeadStockInsights = (
  warehouseId?: string,
  daysWithoutOutbound = 60,
  minOnHand = 1,
  maxResults = 20
) =>
  apiClient
    .get<DeadStockInsight[]>("/api/inventory-insights/dead-stock", {
      params: { warehouseId, daysWithoutOutbound, minOnHand, maxResults },
    })
    .then((r) => r.data);

export const fetchSalesVelocityInsights = (
  warehouseId?: string,
  lookbackDays = 30,
  maxResults = 20
) =>
  apiClient
    .get<SalesVelocityInsight[]>("/api/inventory-insights/sales-velocity", {
      params: { warehouseId, lookbackDays, maxResults },
    })
    .then((r) => r.data);

export const fetchTransferSuggestions = (
  sourceWarehouseId?: string,
  destinationWarehouseId?: string,
  lookbackDays = 30,
  targetCoverDays = 14,
  reserveCoverDays = 7,
  maxResults = 20
) =>
  apiClient
    .get<TransferSuggestion[]>("/api/inventory-insights/transfer-suggestions", {
      params: {
        sourceWarehouseId,
        destinationWarehouseId,
        lookbackDays,
        targetCoverDays,
        reserveCoverDays,
        maxResults,
      },
    })
    .then((r) => r.data);

export const fetchMarkdownCandidates = (
  warehouseId?: string,
  daysWithoutOutbound = 60,
  minOnHand = 1,
  maxResults = 20
) =>
  apiClient
    .get<MarkdownCandidateInsight[]>("/api/inventory-insights/markdown-candidates", {
      params: { warehouseId, daysWithoutOutbound, minOnHand, maxResults },
    })
    .then((r) => r.data);

export const fetchPromotionRiskInsights = (
  warehouseId?: string,
  lookbackDays = 30,
  maxResults = 20
) =>
  apiClient
    .get<PromotionRiskInsight[]>("/api/inventory-insights/promotion-risk", {
      params: { warehouseId, lookbackDays, maxResults },
    })
    .then((r) => r.data);

export const fetchReorderRiskInsights = (
  warehouseId?: string,
  lookbackDays = 30,
  maxResults = 20
) =>
  apiClient
    .get<ReorderRiskInsight[]>("/api/inventory-insights/reorder-risk", {
      params: { warehouseId, lookbackDays, maxResults },
    })
    .then((r) => r.data);

export const fetchTrendSummaryInsights = (
  warehouseId?: string,
  lookbackDays = 30,
  maxResults = 20
) =>
  apiClient
    .get<TrendSummaryInsight[]>("/api/inventory-insights/trend-summary", {
      params: { warehouseId, lookbackDays, maxResults },
    })
    .then((r) => r.data);

export const fetchInsightsExecutiveSummary = (
  warehouseId?: string,
  lookbackDays = 30,
  daysWithoutOutbound = 60
) =>
  apiClient
    .get<InsightsExecutiveSummary>("/api/inventory-insights/executive-summary", {
      params: { warehouseId, lookbackDays, daysWithoutOutbound },
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
