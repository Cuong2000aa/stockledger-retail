import axios from "axios";
import type {
  AdjustmentLineInput,
  CreateProductInput,
  CreateProductVariantInput,
  CreateWarehouseInput,
  CurrentStock,
  DocumentLineInput,
  InventoryDocument,
  InventoryDocumentType,
  PagedResult,
  Product,
  ProductVariant,
  StockTransaction,
  UpdateProductInput,
  UpdateProductVariantInput,
  UpdateWarehouseInput,
  Warehouse,
} from "./types";

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5270",
  headers: { "Content-Type": "application/json" },
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    const message =
      err.response?.data?.error ?? err.message ?? "Request failed";
    return Promise.reject(new Error(message));
  }
);

export function getApiErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unknown error";
}

// Products
export const fetchProducts = (page = 1, pageSize = 20) =>
  api
    .get<PagedResult<Product>>("/api/products", { params: { page, pageSize } })
    .then((r) => r.data);

export const createProduct = (input: CreateProductInput) =>
  api.post<Product>("/api/products", input).then((r) => r.data);

export const updateProduct = (id: string, input: UpdateProductInput) =>
  api.put<Product>(`/api/products/${id}`, input).then((r) => r.data);

export const deleteProduct = (id: string) =>
  api.delete(`/api/products/${id}`);

// Product variants
export const fetchProductVariants = (page = 1, pageSize = 50) =>
  api
    .get<PagedResult<ProductVariant>>("/api/product-variants", {
      params: { page, pageSize },
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
export const fetchWarehouses = (page = 1, pageSize = 50) =>
  api
    .get<PagedResult<Warehouse>>("/api/warehouses", { params: { page, pageSize } })
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
  page = 1,
  pageSize = 20
) =>
  api
    .get<PagedResult<InventoryDocument>>("/api/inventory-documents", {
      params: { documentType, page, pageSize },
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

export const approveDocument = (id: string) =>
  api
    .post<InventoryDocument>(`/api/inventory-documents/${id}/approve`)
    .then((r) => r.data);

export const cancelDocument = (id: string) =>
  api
    .post<InventoryDocument>(`/api/inventory-documents/${id}/cancel`)
    .then((r) => r.data);

// Stocks & transactions
export const fetchCurrentStocks = (
  warehouseId?: string,
  productVariantId?: string,
  page = 1,
  pageSize = 20
) =>
  api
    .get<PagedResult<CurrentStock>>("/api/current-stocks", {
      params: { warehouseId, productVariantId, page, pageSize },
    })
    .then((r) => r.data);

export const fetchStockTransactions = (
  warehouseId?: string,
  productVariantId?: string,
  page = 1,
  pageSize = 20
) =>
  api
    .get<PagedResult<StockTransaction>>("/api/stock-transactions", {
      params: { warehouseId, productVariantId, page, pageSize },
    })
    .then((r) => r.data);
