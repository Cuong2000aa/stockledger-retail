import { apiClient } from "@/lib/api";
import type {
  DeadStockInsight,
  SalesVelocityInsight,
  TransferSuggestion,
} from "./types";

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
