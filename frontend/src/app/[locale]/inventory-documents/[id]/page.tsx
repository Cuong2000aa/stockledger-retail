"use client";

import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { DocStatusBadge, docTypeKey } from "@/components/StatusBadge";
import {
  approveDocument,
  cancelDocument,
  fetchInventoryDocument,
  getApiErrorMessage,
} from "@/lib/api";
import { formatDate, formatNumber } from "@/lib/format";
import { InventoryDocumentStatus } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { use } from "react";

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

  const approveMutation = useMutation({
    mutationFn: () => approveDocument(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory-document", id] });
      qc.invalidateQueries({ queryKey: ["inventory-documents"] });
      qc.invalidateQueries({ queryKey: ["current-stocks"] });
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

  return (
    <div>
      <PageHeader
        title={doc.documentNo}
        subtitle={t("detail")}
        action={
          <Link href="/inventory-documents" className="btn-secondary">
            {tCommon("back")}
          </Link>
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
                <th>{t("quantity")}</th>
                <th>{t("note")}</th>
              </tr>
            </thead>
            <tbody>
              {doc.lines.map((line) => (
                <tr key={line.id}>
                  <td className="font-mono text-xs">{line.sku}</td>
                  <td>{formatNumber(line.quantity, locale)}</td>
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
