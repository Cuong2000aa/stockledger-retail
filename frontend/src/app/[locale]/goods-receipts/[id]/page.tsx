"use client";

import { DataTableCard } from "@/components/DataTableCard";
import { AuditTrailPanel } from "@/components/AuditTrailPanel";
import { GrStatusBadge } from "@/components/StatusBadge";
import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { useAuth } from "@/features/auth/AuthProvider";
import { useNotify } from "@/hooks/useNotify";
import {
  approveGoodsReceipt,
  cancelGoodsReceipt,
  fetchGoodsReceipt,
} from "@/lib/api";
import { formatDate, formatNumber } from "@/lib/format";
import { GoodsReceiptStatus } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  ClipboardList,
  FileText,
  Package,
  Warehouse,
  XCircle,
} from "lucide-react";
import clsx from "clsx";
import { use, useMemo } from "react";

const PERM_APPROVE = "inventory.documents.approve";
const PERM_APPROVE_TEAM = "inventory.documents.approve.team";

function grStatusLabel(
  status: GoodsReceiptStatus,
  t: ReturnType<typeof useTranslations<"goodsReceipts">>
) {
  switch (status) {
    case GoodsReceiptStatus.Draft:
      return t("statusDraft");
    case GoodsReceiptStatus.Approved:
      return t("statusApproved");
    case GoodsReceiptStatus.Cancelled:
      return t("statusCancelled");
    default:
      return String(status);
  }
}

export default function GoodsReceiptDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const t = useTranslations("goodsReceipts");
  const tDoc = useTranslations("documents");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const qc = useQueryClient();
  const { notifyError, confirm } = useNotify();
  const { hasPermission } = useAuth();

  const { data: gr, isLoading } = useQuery({
    queryKey: ["goods-receipt", id],
    queryFn: () => fetchGoodsReceipt(id),
  });

  const canApprove = useMemo(
    () => hasPermission(PERM_APPROVE) || hasPermission(PERM_APPROVE_TEAM),
    [hasPermission]
  );

  const approveMutation = useMutation({
    mutationFn: () => approveGoodsReceipt(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["goods-receipt", id] });
      qc.invalidateQueries({ queryKey: ["purchase-order"] });
      qc.invalidateQueries({ queryKey: ["current-stocks"] });
      qc.invalidateQueries({ queryKey: ["inventory-summary"] });
    },
    onError: notifyError,
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelGoodsReceipt(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["goods-receipt", id] });
    },
    onError: notifyError,
  });

  if (isLoading || !gr) {
    return <p className="text-slate-500">{tCommon("loading")}</p>;
  }

  const isDraft = gr.status === GoodsReceiptStatus.Draft;
  const statusLabel = grStatusLabel(gr.status, t);
  const totalQty = gr.lines.reduce((sum, line) => sum + line.receivedQuantity, 0);

  return (
    <div className="space-y-6">
      <PageHeader
        title={gr.grNo}
        subtitle={t("detail")}
        action={
          <Link href="/goods-receipts" className="btn-secondary">
            <ArrowLeft className="h-4 w-4" />
            {tCommon("back")}
          </Link>
        }
      />

      <div className="card overflow-hidden">
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 bg-gradient-to-r from-slate-50/80 to-white px-6 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-50 text-brand-600 ring-1 ring-brand-100">
              <Package className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">PO</p>
              <Link
                href={`/purchase-orders/${gr.purchaseOrderId}`}
                className="font-mono text-sm font-semibold text-brand-600 hover:underline"
              >
                {gr.poNo}
              </Link>
            </div>
          </div>
          <GrStatusBadge status={gr.status} label={statusLabel} />
        </div>

        <div className="grid gap-6 p-6 sm:grid-cols-2 lg:grid-cols-4">
          <div className="flex gap-3">
            <Warehouse className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
            <div>
              <p className="text-xs text-slate-500">{tDoc("warehouse")}</p>
              <p className="font-medium text-slate-900">{gr.warehouseCode}</p>
            </div>
          </div>
          <div className="flex gap-3">
            <ClipboardList className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
            <div>
              <p className="text-xs text-slate-500">{t("receiptDate")}</p>
              <p className="font-medium text-slate-900">{formatDate(gr.receiptDate, locale)}</p>
            </div>
          </div>
          <div className="flex gap-3">
            <Package className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
            <div>
              <p className="text-xs text-slate-500">{tDoc("lines")}</p>
              <p className="font-medium text-slate-900">
                {gr.lines.length} · {formatNumber(totalQty, locale)} {t("receivedQty").toLowerCase()}
              </p>
            </div>
          </div>
          {gr.inventoryDocumentNo ? (
            <div className="flex gap-3">
              <FileText className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
              <div>
                <p className="text-xs text-slate-500">{t("linkedStockIn")}</p>
                <Link
                  href={`/inventory-documents/${gr.inventoryDocumentId}`}
                  className="font-mono text-sm font-medium text-brand-600 hover:underline"
                >
                  {gr.inventoryDocumentNo}
                </Link>
              </div>
            </div>
          ) : null}
        </div>

        {isDraft && (
          <div className="border-t border-slate-100 bg-slate-50/50 px-6 py-4">
            {!canApprove && (
              <div className="mb-4 flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
                <p>{t("approvePermissionHint")}</p>
              </div>
            )}
            <div className="flex flex-wrap gap-3">
              {canApprove && (
                <button
                  type="button"
                  className="btn-primary"
                  disabled={approveMutation.isPending}
                  onClick={async () => {
                    if (await confirm(t("approveConfirm"))) {
                      approveMutation.mutate();
                    }
                  }}
                >
                  <CheckCircle2 className="h-4 w-4" />
                  {t("approve")}
                </button>
              )}
              <button
                type="button"
                className={clsx(canApprove ? "btn-danger" : "btn-secondary")}
                disabled={cancelMutation.isPending}
                onClick={async () => {
                  if (await confirm(t("cancelConfirm"))) {
                    cancelMutation.mutate();
                  }
                }}
              >
                <XCircle className="h-4 w-4" />
                {tDoc("cancelDoc")}
              </button>
            </div>
          </div>
        )}

        <AuditTrailPanel
          locale={locale}
          fields={{
            createdBy: gr.createdBy,
            createdAt: gr.createdAt,
            updatedBy: gr.updatedBy,
            updatedAt: gr.updatedAt,
            approvedBy: gr.approvedBy,
            approvedAt: gr.approvedAt,
          }}
        />
      </div>

      <DataTableCard title={tDoc("lines")} icon={ClipboardList} count={gr.lines.length}>
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>{tDoc("productVariant")}</th>
                <th className="text-right">{t("receivedQty")}</th>
                <th>{tDoc("note")}</th>
              </tr>
            </thead>
            <tbody>
              {gr.lines.map((line) => (
                <tr key={line.id}>
                  <td className="font-mono text-xs">{line.sku}</td>
                  <td className="text-right font-medium tabular-nums">
                    {formatNumber(line.receivedQuantity, locale)}
                  </td>
                  <td className="text-slate-600">{line.note ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </DataTableCard>
    </div>
  );
}
