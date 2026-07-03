import type { QueryClient } from "@tanstack/react-query";
import {
  fetchCurrentStocks,
  fetchInventoryDocuments,
  fetchInventorySummary,
  fetchLowStock,
  fetchMovementSummary,
  fetchProductVariants,
  fetchProducts,
  fetchPurchaseOrders,
  fetchStockByWarehouse,
  fetchStockTransactions,
  fetchSuppliers,
  fetchWarehouses,
} from "@/lib/api";
import { ANALYTICS_STALE_TIME, LIST_STALE_TIME, MASTER_DATA_STALE_TIME } from "@/lib/query-config";

type Prefetcher = (queryClient: QueryClient) => void;

const prefetchers: Record<string, Prefetcher> = {
  "/": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["inventory-summary"],
      queryFn: fetchInventorySummary,
      staleTime: ANALYTICS_STALE_TIME,
    });
    void qc.prefetchQuery({
      queryKey: ["stock-by-warehouse"],
      queryFn: fetchStockByWarehouse,
      staleTime: ANALYTICS_STALE_TIME,
    });
    void qc.prefetchQuery({
      queryKey: ["movement-summary"],
      queryFn: () => fetchMovementSummary(),
      staleTime: ANALYTICS_STALE_TIME,
    });
    void qc.prefetchQuery({
      queryKey: ["low-stock", 10],
      queryFn: () => fetchLowStock(10),
      staleTime: ANALYTICS_STALE_TIME,
    });
  },
  "/products": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["products", 1, ""],
      queryFn: () => fetchProducts(1, 20),
      staleTime: LIST_STALE_TIME,
    });
  },
  "/product-variants": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["product-variants", 1, ""],
      queryFn: () => fetchProductVariants(1, 50),
      staleTime: LIST_STALE_TIME,
    });
  },
  "/warehouses": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["warehouses", 1, ""],
      queryFn: () => fetchWarehouses(1, 50),
      staleTime: MASTER_DATA_STALE_TIME,
    });
  },
  "/suppliers": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["suppliers", 1, ""],
      queryFn: () => fetchSuppliers(1, 50),
      staleTime: MASTER_DATA_STALE_TIME,
    });
  },
  "/purchase-orders": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["purchase-orders", 1, "", "", ""],
      queryFn: () => fetchPurchaseOrders(undefined, undefined, 1, 20),
      staleTime: LIST_STALE_TIME,
    });
  },
  "/inventory-documents": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["inventory-documents", 1, "", "", ""],
      queryFn: () => fetchInventoryDocuments(undefined, undefined, 1, 20),
      staleTime: LIST_STALE_TIME,
    });
  },
  "/current-stocks": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["current-stocks", 1, "", ""],
      queryFn: () => fetchCurrentStocks(undefined, undefined, 1, 20),
      staleTime: LIST_STALE_TIME,
    });
  },
  "/stock-transactions": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["stock-transactions", 1, "", ""],
      queryFn: () => fetchStockTransactions(undefined, undefined, 1, 20),
      staleTime: LIST_STALE_TIME,
    });
  },
};

export function prefetchRouteData(queryClient: QueryClient, href: string) {
  const prefetch = prefetchers[href];
  if (prefetch) {
    prefetch(queryClient);
  }
}

/** Warm only the routes users open most often after initial load. */
export function prefetchHotNavRoutes(queryClient: QueryClient) {
  ["/", "/products", "/purchase-orders", "/current-stocks"].forEach((href) => {
    prefetchRouteData(queryClient, href);
  });
}
