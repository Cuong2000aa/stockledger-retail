import type {
  BrokenSizeRunInsight,
  DeadStockInsight,
  InsightsExecutiveSummary,
  MarkdownCandidateInsight,
  PromotionRiskInsight,
  ReorderRiskInsight,
  SalesVelocityInsight,
  SeasonClearanceInsight,
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
  | "trend"
  | "brokenSize"
  | "seasonClearance";

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
  BrokenSizeRunInsight,
  DeadStockInsight,
  InsightsExecutiveSummary,
  MarkdownCandidateInsight,
  PromotionRiskInsight,
  ReorderRiskInsight,
  SalesVelocityInsight,
  SeasonClearanceInsight,
  TrendSummaryInsight,
  TransferSuggestion,
};
