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
import {
  fetchDeadStockInsights,
  fetchSalesVelocityInsights,
  fetchTransferSuggestions,
} from "@/features/insights/api";
import { insightQueryKeys } from "@/features/insights/queries";

type Prefetcher = (queryClient: QueryClient) => void;

const prefetchers: Record<string, Prefetcher> = {
  "/": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["inventory-summary"],
      queryFn: fetchInventorySummary,
    });
    void qc.prefetchQuery({
      queryKey: ["stock-by-warehouse"],
      queryFn: fetchStockByWarehouse,
    });
    void qc.prefetchQuery({
      queryKey: ["movement-summary"],
      queryFn: () => fetchMovementSummary(),
    });
    void qc.prefetchQuery({
      queryKey: ["low-stock", 10],
      queryFn: () => fetchLowStock(10),
    });
  },
  "/products": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["products", 1, ""],
      queryFn: () => fetchProducts(1, 20),
    });
  },
  "/product-variants": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["product-variants", 1, ""],
      queryFn: () => fetchProductVariants(1, 50),
    });
    void qc.prefetchQuery({
      queryKey: ["products-all"],
      queryFn: () => fetchProducts(1, 200),
    });
  },
  "/warehouses": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["warehouses", 1, ""],
      queryFn: () => fetchWarehouses(1, 50),
    });
  },
  "/suppliers": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["suppliers", 1, ""],
      queryFn: () => fetchSuppliers(1, 50),
    });
  },
  "/purchase-orders": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["suppliers-all"],
      queryFn: () => fetchSuppliers(1, 200),
    });
    void qc.prefetchQuery({
      queryKey: ["purchase-orders", 1, "", "", ""],
      queryFn: () => fetchPurchaseOrders(undefined, undefined, 1, 20),
    });
  },
  "/insights": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["warehouses", "all-for-insights"],
      queryFn: () => fetchWarehouses(1, 200),
    });
    void qc.prefetchQuery({
      queryKey: insightQueryKeys.deadStock(undefined, 60),
      queryFn: () => fetchDeadStockInsights(undefined, 60, 1, 20),
    });
    void qc.prefetchQuery({
      queryKey: insightQueryKeys.salesVelocity(undefined, 30),
      queryFn: () => fetchSalesVelocityInsights(undefined, 30, 20),
    });
    void qc.prefetchQuery({
      queryKey: insightQueryKeys.transferSuggestions(undefined, undefined, 30),
      queryFn: () => fetchTransferSuggestions(undefined, undefined, 30, 14, 7, 20),
    });
  },
  "/inventory-documents": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["inventory-documents", 1, "", "", ""],
      queryFn: () => fetchInventoryDocuments(undefined, undefined, 1, 20),
    });
  },
  "/current-stocks": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["warehouses-all"],
      queryFn: () => fetchWarehouses(1, 100),
    });
    void qc.prefetchQuery({
      queryKey: ["current-stocks", 1, "", ""],
      queryFn: () => fetchCurrentStocks(undefined, undefined, 1, 20),
    });
  },
  "/stock-transactions": (qc) => {
    void qc.prefetchQuery({
      queryKey: ["warehouses-all"],
      queryFn: () => fetchWarehouses(1, 100),
    });
    void qc.prefetchQuery({
      queryKey: ["stock-transactions", 1, "", ""],
      queryFn: () => fetchStockTransactions(undefined, undefined, 1, 20),
    });
  },
};

export function prefetchRouteData(queryClient: QueryClient, href: string) {
  const prefetch = prefetchers[href];
  if (prefetch) {
    prefetch(queryClient);
  }
}

export function prefetchAllNavRoutes(queryClient: QueryClient) {
  Object.keys(prefetchers).forEach((href) => {
    prefetchRouteData(queryClient, href);
  });
}
