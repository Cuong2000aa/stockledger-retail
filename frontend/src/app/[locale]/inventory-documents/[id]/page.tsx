"use client";

import { AuditTrailPanel } from "@/components/AuditTrailPanel";
import { DataTableCard } from "@/components/DataTableCard";
import { PageHeader } from "@/components/PageHeader";
import { DocStatusBadge, docTypeKey } from "@/components/StatusBadge";
import { useNotify } from "@/hooks/useNotify";
import { Link } from "@/i18n/routing";
import {
  approveDocument,
  cancelDocument,
  fetchCurrentStocks,
  fetchInventoryDocument,
  fetchWarehouses,
  receiveTransfer,
  submitDocumentForApproval,
} from "@/lib/api";
import { formatNumber } from "@/lib/format";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { formatUnitBarcodes } from "@/lib/unitBarcode";
import {
  InventoryDocumentStatus,
  InventoryDocumentType,
  TransferLifecycleStatus,
  type Warehouse,
} from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import {
  ArrowLeft,
  ClipboardList,
  FileText,
  MapPin,
  Tag,
} from "lucide-react";
import { use, useMemo } from "react";

function statusLabel(
  status: InventoryDocumentStatus,
  t: ReturnType<typeof useTranslations<"documents">>
) {
  switch (status) {
    case InventoryDocumentStatus.Draft:
      return t("statusDraft");
    case InventoryDocumentStatus.Pending:
      return t("statusPending");
    case InventoryDocumentStatus.Approved:
      return t("statusApproved");
    case InventoryDocumentStatus.Cancelled:
      return t("statusCancelled");
    case InventoryDocumentStatus.Completed:
      return t("statusCompleted");
    default:
      return String(status);
  }
}

function warehouseLabel(id: string | undefined, map: Map<string, Warehouse>) {
  if (!id) return "—";
  const w = map.get(id);
  return w ? formatWarehouseOptionLabel(w) : id;
}

export default function DocumentDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const t = useTranslations("documents");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const qc = useQueryClient();
  const { notifyError, confirm } = useNotify();

  const { data: doc, isLoading } = useQuery({
    queryKey: ["inventory-document", id],
    queryFn: () => fetchInventoryDocument(id),
  });

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-all"],
    queryFn: () => fetchWarehouses(1, 100),
  });

  const warehouseMap = useMemo(() => {
    const map = new Map<string, Warehouse>();
    warehouses?.items.forEach((w) => map.set(w.id, w));
    return map;
  }, [warehouses]);

  const isStockCount = doc?.documentType === InventoryDocumentType.StockCount;
  const countWarehouseId = doc?.destinationWarehouseId;

  const { data: currentStocks } = useQuery({
    queryKey: ["current-stocks-count", countWarehouseId],
    queryFn: () => fetchCurrentStocks(countWarehouseId, undefined, 1, 500),
    enabled: !!doc && isStockCount && !!countWarehouseId,
  });

  const onHandByVariant = useMemo(() => {
    const map = new Map<string, number>();
    currentStocks?.items.forEach((s) =>
      map.set(s.productVariantId, s.quantityOnHand)
    );
    return map;
  }, [currentStocks]);

  const approveMutation = useMutation({
    mutationFn: () => approveDocument(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory-document", id] });
      qc.invalidateQueries({ queryKey: ["inventory-documents"] });
      qc.invalidateQueries({ queryKey: ["current-stocks"] });
      qc.invalidateQueries({ queryKey: ["stock-transactions"] });
    },
    onError: notifyError,
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelDocument(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory-document", id] });
      qc.invalidateQueries({ queryKey: ["inventory-documents"] });
    },
    onError: notifyError,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitDocumentForApproval(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory-document", id] });
      qc.invalidateQueries({ queryKey: ["inventory-documents"] });
    },
    onError: notifyError,
  });

  const receiveMutation = useMutation({
    mutationFn: () => receiveTransfer(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory-document", id] });
      qc.invalidateQueries({ queryKey: ["inventory-documents"] });
      qc.invalidateQueries({ queryKey: ["current-stocks"] });
    },
    onError: notifyError,
  });

  if (isLoading || !doc) {
    return <p className="text-slate-500">{tCommon("loading")}</p>;
  }

  const isDraft = doc.status === InventoryDocumentStatus.Draft;
  const isPending = doc.status === InventoryDocumentStatus.Pending;
  const isTransfer = doc.documentType === InventoryDocumentType.Transfer;
  const canReceiveTransfer =
    isTransfer &&
    doc.status === InventoryDocumentStatus.Approved &&
    doc.transferLifecycleStatus === TransferLifecycleStatus.Shipped;
  const needsSecondApproval =
    isPending &&
    (doc.requiredApprovalSteps ?? 1) > 1 &&
    (doc.completedApprovalSteps ?? 0) < (doc.requiredApprovalSteps ?? 1);
  const isAdjustment = doc.documentType === InventoryDocumentType.Adjustment;

  const quantityHeader = isStockCount
    ? t("countedQuantity")
    : isAdjustment
      ? t("adjustmentQuantity")
      : t("quantity");

  return (
    <div className="space-y-6">
      <PageHeader
        title={doc.documentNo}
        subtitle={t("detail")}
        action={
          <div className="flex gap-2">
            {isDraft && (
              <Link href={`/inventory-documents/${id}/edit`} className="btn-primary">
                {t("editDraft")}
              </Link>
            )}
            <Link href="/inventory-documents" className="btn-secondary">
              <ArrowLeft className="h-4 w-4" />
              {tCommon("back")}
            </Link>
          </div>
        }
      />

      <div className="card overflow-hidden">
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 bg-gradient-to-r from-slate-50/80 to-white px-6 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-50 text-brand-600 ring-1 ring-brand-100">
              <FileText className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                {t("type")}
              </p>
              <p className="font-medium text-slate-900">
                {t(`types.${docTypeKey(doc.documentType)}` as "types.StockIn")}
              </p>
            </div>
          </div>
          <DocStatusBadge status={doc.status} label={statusLabel(doc.status, t)} />
        </div>

        <div className="grid gap-4 p-6 sm:grid-cols-2 lg:grid-cols-3">
          {doc.referenceNo && (
            <div className="flex gap-3">
              <Tag className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
              <div>
                <p className="text-xs text-slate-500">{t("referenceNo")}</p>
                <p className="font-mono text-sm font-medium text-slate-900">{doc.referenceNo}</p>
              </div>
            </div>
          )}
          {doc.sourceSystem && (
            <div className="flex gap-3">
              <Tag className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
              <div>
                <p className="text-xs text-slate-500">{t("sourceSystem")}</p>
                <p className="text-sm font-medium text-slate-900">{doc.sourceSystem}</p>
              </div>
            </div>
          )}
          {(isTransfer || doc.documentType === InventoryDocumentType.StockOut) &&
            doc.sourceWarehouseId && (
              <div className="flex gap-3 sm:col-span-2">
                <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
                <div>
                  <p className="text-xs text-slate-500">{t("sourceWarehouse")}</p>
                  <p className="text-sm text-slate-900">
                    {warehouseLabel(doc.sourceWarehouseId, warehouseMap)}
                  </p>
                </div>
              </div>
            )}
          {(isTransfer ||
            doc.documentType === InventoryDocumentType.StockIn ||
            isStockCount ||
            isAdjustment) &&
            doc.destinationWarehouseId && (
              <div className="flex gap-3 sm:col-span-2">
                <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
                <div>
                  <p className="text-xs text-slate-500">
                    {isTransfer || doc.documentType === InventoryDocumentType.StockIn
                      ? t("destinationWarehouse")
                      : t("warehouse")}
                  </p>
                  <p className="text-sm text-slate-900">
                    {warehouseLabel(doc.destinationWarehouseId, warehouseMap)}
                  </p>
                </div>
              </div>
            )}
          {doc.note && (
            <div className="flex gap-3 sm:col-span-2 lg:col-span-3">
              <ClipboardList className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
              <div>
                <p className="text-xs text-slate-500">{t("note")}</p>
                <p className="text-sm text-slate-700">{doc.note}</p>
              </div>
            </div>
          )}
          {isTransfer && doc.transferLifecycleStatus !== undefined && (
            <div className="flex gap-3">
              <Tag className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
              <div>
                <p className="text-xs text-slate-500">{t("transferStatus")}</p>
                <p className="text-sm font-medium text-slate-900">
                  {t(
                    `transferLifecycle.${TransferLifecycleStatus[doc.transferLifecycleStatus ?? 0]}` as "transferLifecycle.None"
                  )}
                </p>
              </div>
            </div>
          )}
          {isPending && (
            <div className="flex gap-3">
              <Tag className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
              <div>
                <p className="text-xs text-slate-500">{t("approvalProgress")}</p>
                <p className="text-sm font-medium text-slate-900">
                  {doc.completedApprovalSteps ?? 0} / {doc.requiredApprovalSteps ?? 1}
                </p>
              </div>
            </div>
          )}
        </div>

        {(isDraft || isPending || canReceiveTransfer) && (
          <div className="border-t border-slate-100 bg-slate-50/50 px-6 py-4">
            <div className="flex flex-wrap gap-3">
              {isDraft && (
                <>
                  <button
                    type="button"
                    className="btn-secondary"
                    disabled={submitMutation.isPending}
                    onClick={async () => {
                      if (await confirm(t("submitConfirm"))) submitMutation.mutate();
                    }}
                  >
                    {t("submitForApproval")}
                  </button>
                  <button
                    type="button"
                    className="btn-primary"
                    disabled={approveMutation.isPending}
                    onClick={async () => {
                      if (await confirm(t("approveConfirm"))) approveMutation.mutate();
                    }}
                  >
                    {t("approve")}
                  </button>
                  <button
                    type="button"
                    className="btn-danger"
                    disabled={cancelMutation.isPending}
                    onClick={async () => {
                      if (await confirm(t("cancelConfirm"))) cancelMutation.mutate();
                    }}
                  >
                    {t("cancelDoc")}
                  </button>
                </>
              )}
              {isPending && (
                <>
                  <button
                    type="button"
                    className="btn-primary"
                    disabled={approveMutation.isPending}
                    onClick={async () => {
                      if (
                        await confirm(
                          needsSecondApproval ? t("approveStepConfirm") : t("approveConfirm")
                        )
                      ) {
                        approveMutation.mutate();
                      }
                    }}
                  >
                    {needsSecondApproval ? t("approveStep") : t("approve")}
                  </button>
                  <button
                    type="button"
                    className="btn-danger"
                    disabled={cancelMutation.isPending}
                    onClick={async () => {
                      if (await confirm(t("cancelConfirm"))) cancelMutation.mutate();
                    }}
                  >
                    {t("cancelDoc")}
                  </button>
                </>
              )}
              {canReceiveTransfer && (
                <button
                  type="button"
                  className="btn-primary"
                  disabled={receiveMutation.isPending}
                  onClick={async () => {
                    if (await confirm(t("receiveTransferConfirm"))) receiveMutation.mutate();
                  }}
                >
                  {t("receiveTransfer")}
                </button>
              )}
            </div>
          </div>
        )}

        <AuditTrailPanel
          locale={locale}
          fields={{
            createdBy: doc.createdBy,
            createdAt: doc.createdAt,
            updatedBy: doc.updatedBy,
            updatedAt: doc.updatedAt,
            submittedBy: doc.submittedBy,
            submittedAt: doc.submittedAt,
            approvedBy: doc.approvedBy ?? doc.firstApprovedBy,
            approvedAt: doc.approvedAt ?? doc.firstApprovedAt,
          }}
        />
      </div>

      <DataTableCard title={t("lines")} icon={ClipboardList} count={doc.lines.length}>
        <div className="table-wrap">
          <table className="data-table text-sm">
            <thead>
              <tr>
                <th>{t("productVariant")}</th>
                <th className="text-right">{quantityHeader}</th>
                <th>{t("barcode")}</th>
                {isStockCount && isDraft && (
                  <>
                    <th className="text-right">{t("systemQuantity")}</th>
                    <th className="text-right">{t("variance")}</th>
                  </>
                )}
                <th>{t("note")}</th>
              </tr>
            </thead>
            <tbody>
              {doc.lines.map((line) => {
                const onHand = onHandByVariant.get(line.productVariantId) ?? 0;
                const variance = line.quantity - onHand;
                return (
                  <tr key={line.id}>
                    <td className="font-mono text-xs">{line.sku}</td>
                    <td className="text-right font-medium tabular-nums">
                      {formatNumber(line.quantity, locale)}
                    </td>
                    <td className="max-w-xs font-mono text-xs break-all text-slate-600">
                      {(line.barcodes?.length ?? 0) > 0
                        ? formatUnitBarcodes(line.barcodes!)
                        : "—"}
                    </td>
                    {isStockCount && isDraft && (
                      <>
                        <td className="text-right tabular-nums">
                          {formatNumber(onHand, locale)}
                        </td>
                        <td
                          className={`text-right tabular-nums font-medium ${
                            variance > 0
                              ? "text-emerald-700"
                              : variance < 0
                                ? "text-red-700"
                                : ""
                          }`}
                        >
                          {variance > 0 ? "+" : ""}
                          {formatNumber(variance, locale)}
                        </td>
                      </>
                    )}
                    <td className="text-slate-600">{line.note ?? "—"}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </DataTableCard>
    </div>
  );
}
