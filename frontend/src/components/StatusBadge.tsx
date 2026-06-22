import {
  InventoryDocumentStatus,
  InventoryDocumentType,
  ProductStatus,
  StockTransactionType,
  WarehouseType,
} from "@/lib/types";
import clsx from "clsx";

const statusStyles: Record<InventoryDocumentStatus, string> = {
  [InventoryDocumentStatus.Draft]: "bg-slate-100 text-slate-700",
  [InventoryDocumentStatus.Pending]: "bg-amber-100 text-amber-800",
  [InventoryDocumentStatus.Approved]: "bg-green-100 text-green-800",
  [InventoryDocumentStatus.Completed]: "bg-blue-100 text-blue-800",
  [InventoryDocumentStatus.Cancelled]: "bg-red-100 text-red-800",
};

export function DocStatusBadge({
  status,
  label,
}: {
  status: InventoryDocumentStatus;
  label: string;
}) {
  return (
    <span
      className={clsx(
        "inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium",
        statusStyles[status]
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
        "inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium",
        active ? "bg-green-100 text-green-800" : "bg-slate-100 text-slate-600"
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

export function isProductActive(status: ProductStatus) {
  return status === ProductStatus.Active;
}

export function transactionTypeKey(type: StockTransactionType): string {
  return StockTransactionType[type];
}
