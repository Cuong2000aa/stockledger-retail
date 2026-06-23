import {
  InventoryDocumentStatus,
  InventoryDocumentType,
  ProductStatus,
  StockTransactionType,
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

export function transactionTypeKey(type: StockTransactionType): string {
  return StockTransactionType[type];
}
