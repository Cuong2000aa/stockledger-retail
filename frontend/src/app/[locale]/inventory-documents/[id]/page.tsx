"use client";

import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { DocStatusBadge, docTypeKey } from "@/components/StatusBadge";
import {
  approveDocument,
  cancelDocument,
  fetchCurrentStocks,
  fetchInventoryDocument,
  fetchWarehouses,
  getApiErrorMessage,
} from "@/lib/api";
import { formatDate, formatNumber } from "@/lib/format";
import {
  InventoryDocumentStatus,
  InventoryDocumentType,
} from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { use, useMemo } from "react";

function statusLabel(
  status: InventoryDocumentStatus,
  t: ReturnType<typeof useTranslations<"documents">>
) {
  switch (status) {
    case InventoryDocumentStatus.Draft:
      return t("statusDraft");
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

function warehouseLabel(
  id: string | undefined,
  map: Map<string, { code: string; name: string }>
) {
  if (!id) return "—";
  const w = map.get(id);
  return w ? `${w.code} — ${w.name}` : id;
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

  const { data: doc, isLoading } = useQuery({
    queryKey: ["inventory-document", id],
    queryFn: () => fetchInventoryDocument(id),
  });

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-all"],
    queryFn: () => fetchWarehouses(1, 100),
  });

  const warehouseMap = useMemo(() => {
    const map = new Map<string, { code: string; name: string }>();
    warehouses?.items.forEach((w) => map.set(w.id, { code: w.code, name: w.name }));
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
    onError: (e) => alert(getApiErrorMessage(e)),
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelDocument(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory-document", id] });
      qc.invalidateQueries({ queryKey: ["inventory-documents"] });
    },
    onError: (e) => alert(getApiErrorMessage(e)),
  });

  if (isLoading || !doc) {
    return <p className="text-slate-500">{tCommon("loading")}</p>;
  }

  const isDraft = doc.status === InventoryDocumentStatus.Draft;
  const isTransfer = doc.documentType === InventoryDocumentType.Transfer;
  const isAdjustment = doc.documentType === InventoryDocumentType.Adjustment;

  const quantityHeader = isStockCount
    ? t("countedQuantity")
    : isAdjustment
      ? t("adjustmentQuantity")
      : t("quantity");

  return (
    <div>
      <PageHeader
        title={doc.documentNo}
        subtitle={t("detail")}
        action={
          <div className="flex gap-2">
            {isDraft && (
              <Link
                href={`/inventory-documents/${id}/edit`}
                className="btn-primary"
              >
                {t("editDraft")}
              </Link>
            )}
            <Link href="/inventory-documents" className="btn-secondary">
              {tCommon("back")}
            </Link>
          </div>
        }
      />

      <div className="card mb-6 p-6">
        <dl className="grid gap-4 sm:grid-cols-2">
          <div>
            <dt className="text-xs text-slate-500">{t("type")}</dt>
            <dd className="font-medium">
              {t(`types.${docTypeKey(doc.documentType)}` as "types.StockIn")}
            </dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{tCommon("status")}</dt>
            <dd>
              <DocStatusBadge
                status={doc.status}
                label={statusLabel(doc.status, t)}
              />
            </dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{t("documentDate")}</dt>
            <dd>{formatDate(doc.documentDate, locale)}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">{t("referenceNo")}</dt>
            <dd>{doc.referenceNo ?? "—"}</dd>
          </div>
          {(isTransfer || doc.documentType === InventoryDocumentType.StockOut) &&
            doc.sourceWarehouseId && (
              <div>
                <dt className="text-xs text-slate-500">{t("sourceWarehouse")}</dt>
                <dd>{warehouseLabel(doc.sourceWarehouseId, warehouseMap)}</dd>
              </div>
            )}
          {(isTransfer ||
            doc.documentType === InventoryDocumentType.StockIn ||
            isStockCount ||
            isAdjustment) &&
            doc.destinationWarehouseId && (
              <div>
                <dt className="text-xs text-slate-500">
                  {isTransfer || doc.documentType === InventoryDocumentType.StockIn
                    ? t("destinationWarehouse")
                    : t("warehouse")}
                </dt>
                <dd>
                  {warehouseLabel(doc.destinationWarehouseId, warehouseMap)}
                </dd>
              </div>
            )}
          <div className="sm:col-span-2">
            <dt className="text-xs text-slate-500">{t("note")}</dt>
            <dd>{doc.note ?? "—"}</dd>
          </div>
        </dl>

        {isDraft && (
          <div className="mt-6 flex gap-3 border-t border-slate-100 pt-4">
            <button
              className="btn-primary"
              disabled={approveMutation.isPending}
              onClick={() => {
                if (confirm(t("approveConfirm"))) approveMutation.mutate();
              }}
            >
              {t("approve")}
            </button>
            <button
              className="btn-danger"
              disabled={cancelMutation.isPending}
              onClick={() => {
                if (confirm(t("cancelConfirm"))) cancelMutation.mutate();
              }}
            >
              {t("cancelDoc")}
            </button>
          </div>
        )}
      </div>

      <div className="card">
        <div className="border-b border-slate-200 px-4 py-3 font-medium">
          {t("lines")}
        </div>
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>{t("productVariant")}</th>
                <th>{quantityHeader}</th>
                {isStockCount && isDraft && (
                  <>
                    <th>{t("systemQuantity")}</th>
                    <th>{t("variance")}</th>
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
                    <td>{formatNumber(line.quantity, locale)}</td>
                    {isStockCount && isDraft && (
                      <>
                        <td>{formatNumber(onHand, locale)}</td>
                        <td
                          className={
                            variance > 0
                              ? "text-green-700"
                              : variance < 0
                                ? "text-red-700"
                                : ""
                          }
                        >
                          {variance > 0 ? "+" : ""}
                          {formatNumber(variance, locale)}
                        </td>
                      </>
                    )}
                    <td>{line.note ?? "—"}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
