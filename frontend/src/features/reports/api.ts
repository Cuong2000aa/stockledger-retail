import { apiClient } from "@/lib/api";
import type {
  InventoryValueReport,
  NearExpiryLot,
  NxtReport,
  PagedResult,
  StockReservationListItem,
  StockReservationStatus,
} from "@/lib/types";

export const fetchInventoryValueReport = (warehouseId?: string, brandId?: string) =>
  apiClient
    .get<InventoryValueReport>("/api/reports/inventory-value", {
      params: { warehouseId, brandId },
    })
    .then((r) => r.data);

export const fetchNxtReport = (fromDate: string, toDate: string, warehouseId?: string) =>
  apiClient
    .get<NxtReport>("/api/reports/nxt", {
      params: { fromDate, toDate, warehouseId },
    })
    .then((r) => r.data);

export const fetchNearExpiryLots = (
  daysAhead = 30,
  warehouseId?: string,
  brandId?: string
) =>
  apiClient
    .get<NearExpiryLot[]>("/api/reports/near-expiry-lots", {
      params: { daysAhead, warehouseId, brandId },
    })
    .then((r) => r.data);

export const fetchStockReservations = (
  warehouseId?: string,
  status?: StockReservationStatus,
  page = 1,
  pageSize = 20
) =>
  apiClient
    .get<PagedResult<StockReservationListItem>>("/api/stock-reservations", {
      params: { warehouseId, status, page, pageSize },
    })
    .then((r) => r.data);

export const releaseStockReservation = (id: string) =>
  apiClient.post(`/api/stock-reservations/${id}/release`).then((r) => r.data);

export const runStockReconciliation = () =>
  apiClient.post("/api/inventory/reconciliation/run").then((r) => r.data);
