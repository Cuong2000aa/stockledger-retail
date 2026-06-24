import type {
  DeadStockInsight,
  SalesVelocityInsight,
  TransferSuggestion,
} from "@/lib/types";

export type InsightTab = "deadStock" | "velocity" | "transfer";

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
  SalesVelocityInsight,
  TransferSuggestion,
};
