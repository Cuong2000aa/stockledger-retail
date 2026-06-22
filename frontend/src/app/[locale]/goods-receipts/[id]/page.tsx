"use client";

import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
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
import { use } from "react";

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

  const { data: gr, isLoading } = useQuery({
    queryKey: ["goods-receipt", id],
    queryFn: () => fetchGoodsReceipt(id),
  });

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

  return (
    <div>
      <PageHeader
        title={gr.grNo}
        subtitle={t("detail")}
        action={
          <Link href={`/purchase-orders/${gr.purchaseOrderId}`} className="btn-secondary">
            {tCommon("back")}
          </Link>
        }
      />

      <div className="card mb-6 p-6">
        <dl className="grid gap-4 sm:grid-cols-2">
          <div>
            <dt className="text-xs text-slate-500">PO</dt>
            <dd className="font-mono text-sm">{gr.poNo}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{tDoc("warehouse")}</dt>
            <dd>{gr.warehouseCode}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{tCommon("status")}</dt>
            <dd>
              {gr.status === GoodsReceiptStatus.Draft
                ? t("statusDraft")
                : gr.status === GoodsReceiptStatus.Approved
                  ? t("statusApproved")
                  : t("statusCancelled")}
            </dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{t("receiptDate")}</dt>
            <dd>{formatDate(gr.receiptDate, locale)}</dd>
          </div>
          {gr.inventoryDocumentNo && (
            <div className="sm:col-span-2">
              <dt className="text-xs text-slate-500">{t("linkedStockIn")}</dt>
              <dd>
                <Link
                  href={`/inventory-documents/${gr.inventoryDocumentId}`}
                  className="text-brand-600 hover:underline"
                >
                  {gr.inventoryDocumentNo}
                </Link>
              </dd>
            </div>
          )}
        </dl>

        {isDraft && (
          <div className="mt-6 flex gap-3 border-t border-slate-100 pt-4">
            <button
              className="btn-primary"
              disabled={approveMutation.isPending}
              onClick={async () => {
                if (await confirm(t("approveConfirm"))) {
                  approveMutation.mutate();
                }
              }}
            >
              {t("approve")}
            </button>
            <button
              className="btn-danger"
              disabled={cancelMutation.isPending}
              onClick={async () => {
                if (await confirm(t("cancelConfirm"))) {
                  cancelMutation.mutate();
                }
              }}
            >
              {tDoc("cancelDoc")}
            </button>
          </div>
        )}
      </div>

      <div className="card">
        <div className="border-b border-slate-200 px-4 py-3 font-medium">{tDoc("lines")}</div>
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>{tDoc("productVariant")}</th>
                <th>{t("receivedQty")}</th>
                <th>{tDoc("note")}</th>
              </tr>
            </thead>
            <tbody>
              {gr.lines.map((line) => (
                <tr key={line.id}>
                  <td className="font-mono text-xs">{line.sku}</td>
                  <td>{formatNumber(line.receivedQuantity, locale)}</td>
                  <td>{line.note ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
