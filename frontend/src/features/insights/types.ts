import type {
  DeadStockInsight,
  SalesVelocityInsight,
  TransferSuggestion,
} from "@/lib/types";

export type InsightSeverity = "info" | "warning" | "critical";

export interface InsightFilterState {
  warehouseId?: string;
  lookbackDays: number;
  daysWithoutOutbound: number;
}

export type {
  DeadStockInsight,
  SalesVelocityInsight,
  TransferSuggestion,
};
