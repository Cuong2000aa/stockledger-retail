import type {
  DeadStockInsight,
  InsightsExecutiveSummary,
  MarkdownCandidateInsight,
  PromotionRiskInsight,
  ReorderRiskInsight,
  SalesVelocityInsight,
  TrendSummaryInsight,
  TransferSuggestion,
} from "@/lib/types";

export type InsightTab =
  | "deadStock"
  | "velocity"
  | "transfer"
  | "markdown"
  | "promotionRisk"
  | "reorderRisk"
  | "trend";

export type InsightSeverity = "info" | "warning" | "critical";

export type InsightExplainContext = {
  sku?: string;
  warehouseCode?: string;
  warehouseName?: string;
  sourceWarehouseCode?: string;
  destinationWarehouseCode?: string;
};

export interface InsightFilterState {
  warehouseId?: string;
  lookbackDays: number;
  daysWithoutOutbound: number;
}

export type {
  DeadStockInsight,
  InsightsExecutiveSummary,
  MarkdownCandidateInsight,
  PromotionRiskInsight,
  ReorderRiskInsight,
  SalesVelocityInsight,
  TrendSummaryInsight,
  TransferSuggestion,
};
