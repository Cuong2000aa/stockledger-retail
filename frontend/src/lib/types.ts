export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export enum ProductStatus {
  Active = 1,
  Inactive = 2,
}

export enum WarehouseType {
  Dc = 1,
  Store = 2,
  SubWarehouse = 3,
  Defect = 4,
  Return = 5,
}

export enum WarehouseStatus {
  Active = 1,
  Inactive = 2,
}

export enum InventoryDocumentType {
  StockIn = 1,
  StockOut = 2,
  Transfer = 3,
  Adjustment = 4,
  StockCount = 5,
}

export enum InventoryDocumentStatus {
  Draft = 1,
  Pending = 2,
  Approved = 3,
  Completed = 4,
  Cancelled = 5,
}

export enum StockTransactionType {
  In = 1,
  Out = 2,
  TransferIn = 3,
  TransferOut = 4,
  AdjustmentIn = 5,
  AdjustmentOut = 6,
  CountAdjustmentIn = 7,
  CountAdjustmentOut = 8,
}

export interface Product {
  id: string;
  productCode: string;
  name: string;
  brand?: string;
  category?: string;
  status: ProductStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductInput {
  productCode: string;
  name: string;
  brand?: string;
  category?: string;
  status: ProductStatus;
}

export interface UpdateProductInput {
  name: string;
  brand?: string;
  category?: string;
  status: ProductStatus;
}

export interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  barcode?: string;
  color?: string;
  size?: string;
  season?: string;
  unit?: string;
  status: ProductStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductVariantInput {
  productId: string;
  sku: string;
  barcode?: string;
  color?: string;
  size?: string;
  season?: string;
  unit?: string;
  status: ProductStatus;
}

export interface UpdateProductVariantInput {
  barcode?: string;
  color?: string;
  size?: string;
  season?: string;
  unit?: string;
  status: ProductStatus;
}

export interface Warehouse {
  id: string;
  code: string;
  name: string;
  type: WarehouseType;
  parentWarehouseId?: string;
  status: WarehouseStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWarehouseInput {
  code: string;
  name: string;
  type: WarehouseType;
  parentWarehouseId?: string;
  status: WarehouseStatus;
}

export interface UpdateWarehouseInput {
  name: string;
  type: WarehouseType;
  parentWarehouseId?: string;
  status: WarehouseStatus;
}

export interface InventoryDocumentLine {
  id: string;
  productVariantId: string;
  sku: string;
  quantity: number;
  unitCost?: number;
  note?: string;
}

export interface InventoryDocument {
  id: string;
  documentNo: string;
  documentType: InventoryDocumentType;
  sourceWarehouseId?: string;
  destinationWarehouseId?: string;
  status: InventoryDocumentStatus;
  documentDate: string;
  referenceNo?: string;
  sourceSystem?: string;
  note?: string;
  createdBy: string;
  createdAt: string;
  approvedBy?: string;
  approvedAt?: string;
  lines: InventoryDocumentLine[];
}

export interface DocumentLineInput {
  productVariantId: string;
  quantity: number;
  unitCost?: number;
  note?: string;
}

export interface AdjustmentLineInput {
  productVariantId: string;
  adjustmentQuantity: number;
  note?: string;
}

export interface CurrentStock {
  id: string;
  productVariantId: string;
  sku: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  lastTransactionId?: string;
  lastUpdatedAt: string;
}

export interface StockTransaction {
  id: string;
  transactionNo: string;
  documentId: string;
  productVariantId: string;
  sku: string;
  warehouseId: string;
  warehouseCode: string;
  transactionType: StockTransactionType;
  quantityDelta: number;
  beforeQuantity: number;
  afterQuantity: number;
  transactionDate: string;
  createdBy?: string;
  createdAt: string;
}

export interface ApiError {
  error: string;
}
