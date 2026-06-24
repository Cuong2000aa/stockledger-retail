import {
  InventoryDocumentStatus,
  InventoryDocumentType,
  ProductStatus,
  PurchaseOrderStatus,
  StockTransactionType,
  WarehouseStatus,
  WarehouseType,
  CostSource,
} from "@/lib/types";
import clsx from "clsx";

const statusStyles: Record<InventoryDocumentStatus, string> = {
  [InventoryDocumentStatus.Draft]: "bg-slate-50 text-slate-700 ring-slate-200",
  [InventoryDocumentStatus.Pending]: "bg-amber-50 text-amber-800 ring-amber-200",
  [InventoryDocumentStatus.Approved]: "bg-emerald-50 text-emerald-800 ring-emerald-200",
  [InventoryDocumentStatus.Completed]: "bg-sky-50 text-sky-800 ring-sky-200",
  [InventoryDocumentStatus.Cancelled]: "bg-red-50 text-red-800 ring-red-200",
};

const poStatusStyles: Record<PurchaseOrderStatus, string> = {
  [PurchaseOrderStatus.Draft]: "bg-slate-50 text-slate-700 ring-slate-200",
  [PurchaseOrderStatus.Submitted]: "bg-sky-50 text-sky-800 ring-sky-200",
  [PurchaseOrderStatus.PartiallyReceived]: "bg-amber-50 text-amber-800 ring-amber-200",
  [PurchaseOrderStatus.Received]: "bg-emerald-50 text-emerald-800 ring-emerald-200",
  [PurchaseOrderStatus.Cancelled]: "bg-red-50 text-red-800 ring-red-200",
  [PurchaseOrderStatus.PendingApproval]: "bg-violet-50 text-violet-800 ring-violet-200",
};

const transactionTypeStyles: Partial<Record<StockTransactionType, string>> = {
  [StockTransactionType.In]: "bg-emerald-50 text-emerald-800 ring-emerald-200",
  [StockTransactionType.Out]: "bg-red-50 text-red-800 ring-red-200",
  [StockTransactionType.TransferIn]: "bg-sky-50 text-sky-800 ring-sky-200",
  [StockTransactionType.TransferOut]: "bg-violet-50 text-violet-800 ring-violet-200",
  [StockTransactionType.AdjustmentIn]: "bg-amber-50 text-amber-800 ring-amber-200",
  [StockTransactionType.AdjustmentOut]: "bg-orange-50 text-orange-800 ring-orange-200",
  [StockTransactionType.CountAdjustmentIn]: "bg-teal-50 text-teal-800 ring-teal-200",
  [StockTransactionType.CountAdjustmentOut]: "bg-rose-50 text-rose-800 ring-rose-200",
};

const warehouseTypeStyles: Partial<Record<WarehouseType, string>> = {
  [WarehouseType.Store]: "bg-indigo-50 text-indigo-800 ring-indigo-200",
  [WarehouseType.Dc]: "bg-sky-50 text-sky-800 ring-sky-200",
  [WarehouseType.SubWarehouse]: "bg-violet-50 text-violet-800 ring-violet-200",
  [WarehouseType.Defect]: "bg-red-50 text-red-800 ring-red-200",
  [WarehouseType.Return]: "bg-amber-50 text-amber-800 ring-amber-200",
};

export function DocStatusBadge({
  status,
  label,
}: {
  status: InventoryDocumentStatus;
  label: string;
}) {
  return (
    <span className={clsx("badge", statusStyles[status])}>{label}</span>
  );
}

export function PoStatusBadge({
  status,
  label,
}: {
  status: PurchaseOrderStatus;
  label: string;
}) {
  return (
    <span className={clsx("badge", poStatusStyles[status])}>{label}</span>
  );
}

export function TransactionTypeBadge({
  type,
  label,
}: {
  type: StockTransactionType;
  label: string;
}) {
  return (
    <span
      className={clsx(
        "badge",
        transactionTypeStyles[type] ?? "bg-slate-50 text-slate-700 ring-slate-200"
      )}
    >
      {label}
    </span>
  );
}

export function WarehouseTypeBadge({
  type,
  label,
}: {
  type: WarehouseType;
  label: string;
}) {
  return (
    <span
      className={clsx(
        "badge",
        warehouseTypeStyles[type] ?? "bg-slate-50 text-slate-700 ring-slate-200"
      )}
    >
      {label}
    </span>
  );
}

export function ActiveBadge({ active, label }: { active: boolean; label: string }) {
  return (
    <span
      className={clsx(
        "badge",
        active
          ? "bg-emerald-50 text-emerald-800 ring-emerald-200"
          : "bg-slate-50 text-slate-600 ring-slate-200"
      )}
    >
      {label}
    </span>
  );
}

export function docTypeKey(type: InventoryDocumentType): string {
  return InventoryDocumentType[type];
}

export function warehouseTypeKey(type: WarehouseType): string {
  return WarehouseType[type];
}

export function costSourceKey(source: CostSource): string {
  return CostSource[source];
}

export function isProductActive(status: ProductStatus) {
  return status === ProductStatus.Active;
}

export function isWarehouseActive(status: WarehouseStatus) {
  return status === WarehouseStatus.Active;
}

export function transactionTypeKey(type: StockTransactionType): string {
  return StockTransactionType[type];
}
