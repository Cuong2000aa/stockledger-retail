import axios from "axios";
import { clearAuthSession, getAuthSession } from "./auth-session";
import type {
  AdjustmentLineInput,
  CreateProductInput,
  CreateProductVariantInput,
  CreatePurchaseOrderLineInput,
  CreateSupplierInput,
  CreateWarehouseInput,
  CurrentStock,
  DocumentLineInput,
  GoodsReceipt,
  GoodsReceiptStatus,
  InventoryDocument,
  InventoryDocumentStatus,
  InventoryDocumentType,
  InventorySummary,
  LowStockItem,
  MovementSummary,
  PagedResult,
  Product,
  ProductVariant,
  PurchaseOrder,
  PurchaseOrderStatus,
  StockByWarehouse,
  StockCountLineInput,
  StockTransaction,
  Supplier,
  UpdateDocumentDraftInput,
  UpdateProductInput,
  UpdateProductVariantInput,
  UpdateSupplierInput,
  UpdateWarehouseInput,
  Warehouse,
} from "./types";

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5270",
  headers: { "Content-Type": "application/json" },
});

api.interceptors.request.use((config) => {
  const session = getAuthSession();
  if (session?.email) {
    config.headers["X-User-Email"] = session.email;
  }
  return config;
});

export const apiClient = api;

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401 && typeof window !== "undefined") {
      const isLoginRequest = err.config?.url?.includes("/api/auth/login");
      if (!isLoginRequest) {
        clearAuthSession();
        const locale = window.location.pathname.split("/")[1] || "vi";
        if (!window.location.pathname.includes("/login")) {
          window.location.href = `/${locale}/login`;
        }
      }
    }

    const message =
      err.response?.data?.error ?? err.message ?? "Request failed";
    return Promise.reject(new Error(message));
  }
);

export function getApiErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  if (
    typeof error === "object" &&
    error !== null &&
    "response" in error &&
    typeof (error as { response?: { data?: { error?: string } } }).response?.data
      ?.error === "string"
  ) {
    return (error as { response: { data: { error: string } } }).response.data.error;
  }

  return "Unknown error";
}

// Products
export const fetchProducts = (page = 1, pageSize = 20, search?: string) =>
  api
    .get<PagedResult<Product>>("/api/products", {
      params: { page, pageSize, search: search || undefined },
    })
    .then((r) => r.data);

export const createProduct = (input: CreateProductInput) =>
  api.post<Product>("/api/products", input).then((r) => r.data);

export const updateProduct = (id: string, input: UpdateProductInput) =>
  api.put<Product>(`/api/products/${id}`, input).then((r) => r.data);

export const deleteProduct = (id: string) =>
  api.delete(`/api/products/${id}`);

// Product variants
export const fetchProductVariants = (page = 1, pageSize = 50, search?: string) =>
  api
    .get<PagedResult<ProductVariant>>("/api/product-variants", {
      params: { page, pageSize, search: search || undefined },
    })
    .then((r) => r.data);

export const createProductVariant = (input: CreateProductVariantInput) =>
  api.post<ProductVariant>("/api/product-variants", input).then((r) => r.data);

export const updateProductVariant = (
  id: string,
  input: UpdateProductVariantInput
) =>
  api
    .put<ProductVariant>(`/api/product-variants/${id}`, input)
    .then((r) => r.data);

export const deleteProductVariant = (id: string) =>
  api.delete(`/api/product-variants/${id}`);

// Warehouses
export const fetchWarehouses = (page = 1, pageSize = 50, search?: string) =>
  api
    .get<PagedResult<Warehouse>>("/api/warehouses", {
      params: { page, pageSize, search: search || undefined },
    })
    .then((r) => r.data);

export const createWarehouse = (input: CreateWarehouseInput) =>
  api.post<Warehouse>("/api/warehouses", input).then((r) => r.data);

export const updateWarehouse = (id: string, input: UpdateWarehouseInput) =>
  api.put<Warehouse>(`/api/warehouses/${id}`, input).then((r) => r.data);

export const deleteWarehouse = (id: string) =>
  api.delete(`/api/warehouses/${id}`);

// Inventory documents
export const fetchInventoryDocuments = (
  documentType?: InventoryDocumentType,
  status?: InventoryDocumentStatus,
  page = 1,
  pageSize = 20,
  search?: string
) =>
  api
    .get<PagedResult<InventoryDocument>>("/api/inventory-documents", {
      params: {
        documentType,
        status,
        page,
        pageSize,
        search: search || undefined,
      },
    })
    .then((r) => r.data);

export const fetchInventoryDocument = (id: string) =>
  api
    .get<InventoryDocument>(`/api/inventory-documents/${id}`)
    .then((r) => r.data);

export const createStockIn = (body: {
  destinationWarehouseId: string;
  documentDate?: string;
  referenceNo?: string;
  note?: string;
  lines: DocumentLineInput[];
}) =>
  api
    .post<InventoryDocument>("/api/inventory-documents/stock-in", body)
    .then((r) => r.data);

export const createStockOut = (body: {
  sourceWarehouseId: string;
  documentDate?: string;
  referenceNo?: string;
  note?: string;
  lines: DocumentLineInput[];
}) =>
  api
    .post<InventoryDocument>("/api/inventory-documents/stock-out", body)
    .then((r) => r.data);

export const createAdjustment = (body: {
  warehouseId: string;
  reason: string;
  documentDate?: string;
  referenceNo?: string;
  note?: string;
  lines: AdjustmentLineInput[];
}) =>
  api
    .post<InventoryDocument>("/api/inventory-documents/adjustment", body)
    .then((r) => r.data);

export const createTransfer = (body: {
  sourceWarehouseId: string;
  destinationWarehouseId: string;
  documentDate?: string;
  referenceNo?: string;
  note?: string;
  lines: DocumentLineInput[];
}) =>
  api
    .post<InventoryDocument>("/api/inventory-documents/transfer", body)
    .then((r) => r.data);

export const createStockCount = (body: {
  warehouseId: string;
  documentDate?: string;
  referenceNo?: string;
  note?: string;
  lines: StockCountLineInput[];
}) =>
  api
    .post<InventoryDocument>("/api/inventory-documents/stock-count", body)
    .then((r) => r.data);

export const updateDocumentDraft = (id: string, body: UpdateDocumentDraftInput) =>
  api
    .put<InventoryDocument>(`/api/inventory-documents/${id}`, body)
    .then((r) => r.data);

export const approveDocument = (id: string) =>
  api
    .post<InventoryDocument>(`/api/inventory-documents/${id}/approve`)
    .then((r) => r.data);

export const cancelDocument = (id: string) =>
  api
    .post<InventoryDocument>(`/api/inventory-documents/${id}/cancel`)
    .then((r) => r.data);

export const submitDocumentForApproval = (id: string) =>
  api
    .post<InventoryDocument>(`/api/inventory-documents/${id}/submit-for-approval`)
    .then((r) => r.data);

export const receiveTransfer = (id: string) =>
  api
    .post<InventoryDocument>(`/api/inventory-documents/${id}/receive-transfer`)
    .then((r) => r.data);

export const approvePurchaseOrder = (id: string) =>
  api.post<PurchaseOrder>(`/api/purchase-orders/${id}/approve`).then((r) => r.data);

// Stocks & transactions
export const fetchCurrentStocks = (
  warehouseId?: string,
  productVariantId?: string,
  page = 1,
  pageSize = 20,
  search?: string
) =>
  api
    .get<PagedResult<CurrentStock>>("/api/current-stocks", {
      params: {
        warehouseId,
        productVariantId,
        page,
        pageSize,
        search: search || undefined,
      },
    })
    .then((r) => r.data);

export const fetchStockTransactions = (
  warehouseId?: string,
  productVariantId?: string,
  page = 1,
  pageSize = 20,
  search?: string
) =>
  api
    .get<PagedResult<StockTransaction>>("/api/stock-transactions", {
      params: {
        warehouseId,
        productVariantId,
        page,
        pageSize,
        search: search || undefined,
      },
    })
    .then((r) => r.data);

// Suppliers
export const fetchSuppliers = (page = 1, pageSize = 50, search?: string) =>
  api
    .get<PagedResult<Supplier>>("/api/suppliers", {
      params: { page, pageSize, search: search || undefined },
    })
    .then((r) => r.data);

export const createSupplier = (input: CreateSupplierInput) =>
  api.post<Supplier>("/api/suppliers", input).then((r) => r.data);

export const updateSupplier = (id: string, input: UpdateSupplierInput) =>
  api.put<Supplier>(`/api/suppliers/${id}`, input).then((r) => r.data);

export const deleteSupplier = (id: string) => api.delete(`/api/suppliers/${id}`);

// Purchase orders
export const fetchPurchaseOrders = (
  status?: PurchaseOrderStatus,
  supplierId?: string,
  page = 1,
  pageSize = 20,
  search?: string
) =>
  api
    .get<PagedResult<PurchaseOrder>>("/api/purchase-orders", {
      params: {
        status,
        supplierId,
        page,
        pageSize,
        search: search || undefined,
      },
    })
    .then((r) => r.data);

export const fetchPurchaseOrder = (id: string) =>
  api.get<PurchaseOrder>(`/api/purchase-orders/${id}`).then((r) => r.data);

export const createPurchaseOrder = (body: {
  supplierId: string;
  warehouseId: string;
  orderDate?: string;
  expectedDate?: string;
  referenceNo?: string;
  note?: string;
  lines: CreatePurchaseOrderLineInput[];
}) => api.post<PurchaseOrder>("/api/purchase-orders", body).then((r) => r.data);

export const submitPurchaseOrder = (id: string) =>
  api.post<PurchaseOrder>(`/api/purchase-orders/${id}/submit`).then((r) => r.data);

export const cancelPurchaseOrder = (id: string) =>
  api.post<PurchaseOrder>(`/api/purchase-orders/${id}/cancel`).then((r) => r.data);

// Goods receipts
export const fetchGoodsReceipts = (
  purchaseOrderId?: string,
  status?: GoodsReceiptStatus,
  page = 1,
  pageSize = 20
) =>
  api
    .get<PagedResult<GoodsReceipt>>("/api/goods-receipts", {
      params: { purchaseOrderId, status, page, pageSize },
    })
    .then((r) => r.data);

export const fetchGoodsReceipt = (id: string) =>
  api.get<GoodsReceipt>(`/api/goods-receipts/${id}`).then((r) => r.data);

export const createGoodsReceipt = (body: {
  purchaseOrderId: string;
  receiptDate?: string;
  referenceNo?: string;
  note?: string;
  lines: { purchaseOrderLineId: string; receivedQuantity: number; note?: string }[];
}) => api.post<GoodsReceipt>("/api/goods-receipts", body).then((r) => r.data);

export const approveGoodsReceipt = (id: string) =>
  api.post<GoodsReceipt>(`/api/goods-receipts/${id}/approve`).then((r) => r.data);

export const cancelGoodsReceipt = (id: string) =>
  api.post<GoodsReceipt>(`/api/goods-receipts/${id}/cancel`).then((r) => r.data);

// Analytics
export const fetchInventorySummary = () =>
  api.get<InventorySummary>("/api/analytics/summary").then((r) => r.data);

export const fetchStockByWarehouse = () =>
  api.get<StockByWarehouse[]>("/api/analytics/stock-by-warehouse").then((r) => r.data);

export const fetchMovementSummary = (fromDate?: string, toDate?: string) =>
  api
    .get<MovementSummary>("/api/analytics/movements", {
      params: { fromDate, toDate },
    })
    .then((r) => r.data);

export const fetchLowStock = (threshold = 10) =>
  api
    .get<LowStockItem[]>("/api/analytics/low-stock", { params: { threshold } })
    .then((r) => r.data);
