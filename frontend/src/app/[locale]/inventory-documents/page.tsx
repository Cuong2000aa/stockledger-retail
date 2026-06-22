"use client";

import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { DocStatusBadge, docTypeKey } from "@/components/StatusBadge";
import { fetchInventoryDocuments } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { InventoryDocumentStatus } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

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

export default function InventoryDocumentsPage() {
  const t = useTranslations("documents");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ["inventory-documents", page],
    queryFn: () => fetchInventoryDocuments(undefined, page),
  });

  return (
    <div>
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <div className="flex flex-wrap gap-2">
            <Link href="/inventory-documents/new/stock-in" className="btn-primary">
              + {t("createStockIn")}
            </Link>
            <Link href="/inventory-documents/new/stock-out" className="btn-secondary">
              + {t("createStockOut")}
            </Link>
            <Link
              href="/inventory-documents/new/adjustment"
              className="btn-secondary"
            >
              + {t("createAdjustment")}
            </Link>
          </div>
        }
      />

      <div className="card">
        {isLoading ? (
          <p className="p-6 text-slate-500">{tCommon("loading")}</p>
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>{t("documentNo")}</th>
                    <th>{t("type")}</th>
                    <th>{t("documentDate")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{t("referenceNo")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.map((doc) => (
                    <tr key={doc.id}>
                      <td className="font-mono text-xs">{doc.documentNo}</td>
                      <td>
                        {t(`types.${docTypeKey(doc.documentType)}` as "types.StockIn")}
                      </td>
                      <td>{formatDate(doc.documentDate, locale)}</td>
                      <td>
                        <DocStatusBadge
                          status={doc.status}
                          label={statusLabel(doc.status, t)}
                        />
                      </td>
                      <td>{doc.referenceNo ?? "—"}</td>
                      <td>
                        <Link
                          href={`/inventory-documents/${doc.id}`}
                          className="text-brand-600 hover:underline"
                        >
                          {t("detail")}
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {data && (
              <Pagination
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                onChange={setPage}
              />
            )}
          </>
        )}
      </div>
    </div>
  );
}
