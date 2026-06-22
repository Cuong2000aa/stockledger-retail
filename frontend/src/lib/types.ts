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

export enum CostSource {
  Manual = 1,
  Erp = 2,
  Pos = 3,
  PurchaseSystem = 4,
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
  costPrice?: number;
  sellingPrice?: number;
  costSource?: CostSource;
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
  costPrice?: number;
  sellingPrice?: number;
  costSource?: CostSource;
}

export interface UpdateProductVariantInput {
  barcode?: string;
  color?: string;
  size?: string;
  season?: string;
  unit?: string;
  status: ProductStatus;
  costPrice?: number;
  sellingPrice?: number;
  costSource?: CostSource;
}

export interface Warehouse {
  id: string;
  code: string;
  name: string;
  type: WarehouseType;
  parentWarehouseId?: string;
  status: WarehouseStatus;
  addressLine?: string;
  ward?: string;
  district?: string;
  province?: string;
  postalCode?: string;
  phone?: string;
  contactName?: string;
  fullAddress?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWarehouseInput {
  code: string;
  name: string;
  type: WarehouseType;
  parentWarehouseId?: string;
  status: WarehouseStatus;
  addressLine?: string;
  ward?: string;
  district?: string;
  province?: string;
  postalCode?: string;
  phone?: string;
  contactName?: string;
}

export interface UpdateWarehouseInput {
  name: string;
  type: WarehouseType;
  parentWarehouseId?: string;
  status: WarehouseStatus;
  addressLine?: string;
  ward?: string;
  district?: string;
  province?: string;
  postalCode?: string;
  phone?: string;
  contactName?: string;
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

export interface StockCountLineInput {
  productVariantId: string;
  countedQuantity: number;
  note?: string;
}

export interface UpdateDocumentDraftInput {
  documentDate?: string;
  referenceNo?: string;
  note?: string;
  lines?: DocumentLineInput[];
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

export enum SupplierStatus {
  Active = 1,
  Inactive = 2,
}

export enum PurchaseOrderStatus {
  Draft = 1,
  Submitted = 2,
  PartiallyReceived = 3,
  Received = 4,
  Cancelled = 5,
}

export enum GoodsReceiptStatus {
  Draft = 1,
  Approved = 2,
  Cancelled = 3,
}

export interface Supplier {
  id: string;
  code: string;
  name: string;
  contactName?: string;
  phone?: string;
  email?: string;
  address?: string;
  status: SupplierStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSupplierInput {
  code: string;
  name: string;
  contactName?: string;
  phone?: string;
  email?: string;
  address?: string;
  status: SupplierStatus;
}

export interface UpdateSupplierInput {
  name: string;
  contactName?: string;
  phone?: string;
  email?: string;
  address?: string;
  status: SupplierStatus;
}

export interface PurchaseOrderLine {
  id: string;
  productVariantId: string;
  sku: string;
  orderedQuantity: number;
  receivedQuantity: number;
  remainingQuantity: number;
  unitCost?: number;
  note?: string;
}

export interface PurchaseOrder {
  id: string;
  poNo: string;
  supplierId: string;
  supplierCode: string;
  supplierName: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  status: PurchaseOrderStatus;
  orderDate: string;
  expectedDate?: string;
  referenceNo?: string;
  note?: string;
  createdBy: string;
  createdAt: string;
  submittedAt?: string;
  lines: PurchaseOrderLine[];
}

export interface CreatePurchaseOrderLineInput {
  productVariantId: string;
  orderedQuantity: number;
  unitCost?: number;
  note?: string;
}

export interface GoodsReceiptLine {
  id: string;
  purchaseOrderLineId: string;
  productVariantId: string;
  sku: string;
  receivedQuantity: number;
  unitCost?: number;
  note?: string;
}

export interface GoodsReceipt {
  id: string;
  grNo: string;
  purchaseOrderId: string;
  poNo: string;
  warehouseId: string;
  warehouseCode: string;
  status: GoodsReceiptStatus;
  receiptDate: string;
  referenceNo?: string;
  note?: string;
  inventoryDocumentId?: string;
  inventoryDocumentNo?: string;
  createdBy: string;
  createdAt: string;
  approvedBy?: string;
  approvedAt?: string;
  lines: GoodsReceiptLine[];
}

export interface InventorySummary {
  totalSkus: number;
  totalOnHand: number;
  totalAvailable: number;
  warehouseCount: number;
  openPurchaseOrders: number;
  pendingGoodsReceipts: number;
}

export interface StockByWarehouse {
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  skuCount: number;
  totalOnHand: number;
  totalAvailable: number;
}

export interface MovementSummary {
  fromDate: string;
  toDate: string;
  totalIn: number;
  totalOut: number;
  transactionCount: number;
}

export interface LowStockItem {
  productVariantId: string;
  sku: string;
  warehouseId: string;
  warehouseCode: string;
  quantityOnHand: number;
  quantityAvailable: number;
}

export interface DeadStockInsight {
  productVariantId: string;
  sku: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  quantityOnHand: number;
  quantityAvailable: number;
  lastOutboundAt?: string;
  daysWithoutOutbound: number;
  costPrice?: number;
  estimatedCostValue?: number;
  severity: string;
  ruleCode: string;
  recommendedActionCode: string;
  recommendationParams: Record<string, string>;
}

export interface SalesVelocityInsight {
  productVariantId: string;
  sku: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  quantityOnHand: number;
  quantityAvailable: number;
  outboundQuantity: number;
  averageDailyOutbound: number;
  estimatedDaysOfCover?: number;
  lastOutboundAt?: string;
  lookbackDays: number;
  severity: string;
  ruleCode: string;
  recommendedActionCode: string;
  recommendationParams: Record<string, string>;
}

export interface TransferSuggestion {
  productVariantId: string;
  sku: string;
  sourceWarehouseId: string;
  sourceWarehouseCode: string;
  sourceWarehouseName: string;
  destinationWarehouseId: string;
  destinationWarehouseCode: string;
  destinationWarehouseName: string;
  suggestedQuantity: number;
  sourceAvailable: number;
  destinationAvailable: number;
  destinationAverageDailyOutbound: number;
  destinationDaysOfCover?: number;
  severity: string;
  ruleCode: string;
  recommendedActionCode: string;
  recommendationParams: Record<string, string>;
}
