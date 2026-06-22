"use client";

import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import {
  cancelPurchaseOrder,
  fetchGoodsReceipts,
  fetchPurchaseOrder,
  submitPurchaseOrder,
} from "@/lib/api";
import { formatDate, formatNumber } from "@/lib/format";
import { GoodsReceiptStatus, PurchaseOrderStatus } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { use } from "react";

function poStatusLabel(
  status: PurchaseOrderStatus,
  t: ReturnType<typeof useTranslations<"purchaseOrders">>
) {
  switch (status) {
    case PurchaseOrderStatus.Draft: return t("statusDraft");
    case PurchaseOrderStatus.Submitted: return t("statusSubmitted");
    case PurchaseOrderStatus.PartiallyReceived: return t("statusPartiallyReceived");
    case PurchaseOrderStatus.Received: return t("statusReceived");
    case PurchaseOrderStatus.Cancelled: return t("statusCancelled");
    default: return String(status);
  }
}

export default function PurchaseOrderDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const t = useTranslations("purchaseOrders");
  const tDoc = useTranslations("documents");
  const tGr = useTranslations("goodsReceipts");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const qc = useQueryClient();
  const { notifyError, confirm } = useNotify();

  const { data: po, isLoading } = useQuery({
    queryKey: ["purchase-order", id],
    queryFn: () => fetchPurchaseOrder(id),
  });

  const { data: receipts } = useQuery({
    queryKey: ["goods-receipts", id],
    queryFn: () => fetchGoodsReceipts(id),
    enabled: !!po,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitPurchaseOrder(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["purchase-order", id] });
      qc.invalidateQueries({ queryKey: ["purchase-orders"] });
    },
    onError: notifyError,
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelPurchaseOrder(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["purchase-order", id] });
      qc.invalidateQueries({ queryKey: ["purchase-orders"] });
    },
    onError: notifyError,
  });

  if (isLoading || !po) {
    return <p className="text-slate-500">{tCommon("loading")}</p>;
  }

  const canSubmit = po.status === PurchaseOrderStatus.Draft;
  const canCancel =
    po.status === PurchaseOrderStatus.Draft ||
    po.status === PurchaseOrderStatus.Submitted;
  const canReceive =
    po.status === PurchaseOrderStatus.Submitted ||
    po.status === PurchaseOrderStatus.PartiallyReceived;

  return (
    <div>
      <PageHeader
        title={po.poNo}
        subtitle={t("detail")}
        action={
          <Link href="/purchase-orders" className="btn-secondary">
            {tCommon("back")}
          </Link>
        }
      />

      <div className="card mb-6 p-6">
        <dl className="grid gap-4 sm:grid-cols-2">
          <div>
            <dt className="text-xs text-slate-500">{t("supplier")}</dt>
            <dd>{po.supplierCode} — {po.supplierName}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{tDoc("warehouse")}</dt>
            <dd>{po.warehouseCode} — {po.warehouseName}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{tCommon("status")}</dt>
            <dd>{poStatusLabel(po.status, t)}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{t("orderDate")}</dt>
            <dd>{formatDate(po.orderDate, locale)}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{tDoc("referenceNo")}</dt>
            <dd>{po.referenceNo ?? "—"}</dd>
          </div>
          <div className="sm:col-span-2">
            <dt className="text-xs text-slate-500">{tDoc("note")}</dt>
            <dd>{po.note ?? "—"}</dd>
          </div>
        </dl>

        <div className="mt-6 flex flex-wrap gap-3 border-t border-slate-100 pt-4">
          {canSubmit && (
            <button
              className="btn-primary"
              disabled={submitMutation.isPending}
              onClick={async () => {
                if (await confirm(t("submitConfirm"))) {
                  submitMutation.mutate();
                }
              }}
            >
              {t("submit")}
            </button>
          )}
          {canReceive && (
            <Link href={`/purchase-orders/${id}/receive`} className="btn-primary">
              {t("receive")}
            </Link>
          )}
          {canCancel && (
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
          )}
        </div>
      </div>

      <div className="card mb-6">
        <div className="border-b border-slate-200 px-4 py-3 font-medium">{tDoc("lines")}</div>
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>{tDoc("productVariant")}</th>
                <th>{t("orderedQty")}</th>
                <th>{t("receivedQty")}</th>
                <th>{t("remainingQty")}</th>
                <th>{t("unitCost")}</th>
              </tr>
            </thead>
            <tbody>
              {po.lines.map((line) => (
                <tr key={line.id}>
                  <td className="font-mono text-xs">{line.sku}</td>
                  <td>{formatNumber(line.orderedQuantity, locale)}</td>
                  <td>{formatNumber(line.receivedQuantity, locale)}</td>
                  <td>{formatNumber(line.remainingQuantity, locale)}</td>
                  <td>{line.unitCost != null ? formatNumber(line.unitCost, locale) : "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {receipts && receipts.items.length > 0 && (
        <div className="card">
          <div className="border-b border-slate-200 px-4 py-3 font-medium">{tGr("title")}</div>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tGr("grNo")}</th>
                  <th>{tGr("receiptDate")}</th>
                  <th>{tCommon("status")}</th>
                  <th>{tCommon("actions")}</th>
                </tr>
              </thead>
              <tbody>
                {receipts.items.map((gr) => (
                  <tr key={gr.id}>
                    <td className="font-mono text-xs">{gr.grNo}</td>
                    <td>{formatDate(gr.receiptDate, locale)}</td>
                    <td>
                      {gr.status === GoodsReceiptStatus.Draft
                        ? tGr("statusDraft")
                        : gr.status === GoodsReceiptStatus.Approved
                          ? tGr("statusApproved")
                          : tGr("statusCancelled")}
                    </td>
                    <td>
                      <Link
                        href={`/goods-receipts/${gr.id}`}
                        className="text-brand-600 hover:underline"
                      >
                        {tGr("detail")}
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
