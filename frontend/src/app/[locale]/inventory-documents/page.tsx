"use client";

import { Link } from "@/i18n/routing";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { DocStatusBadge, docTypeKey } from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { fetchInventoryDocuments } from "@/lib/api";
import { formatDate } from "@/lib/format";
import {
  InventoryDocumentStatus,
  InventoryDocumentType,
} from "@/lib/types";
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

const documentTypes = Object.values(InventoryDocumentType).filter(
  (v) => typeof v === "number"
) as InventoryDocumentType[];

const documentStatuses = Object.values(InventoryDocumentStatus).filter(
  (v) => typeof v === "number"
) as InventoryDocumentStatus[];

export default function InventoryDocumentsPage() {
  const t = useTranslations("documents");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [typeFilter, setTypeFilter] = useState<InventoryDocumentType | "">("");
  const [statusFilter, setStatusFilter] = useState<InventoryDocumentStatus | "">(
    ""
  );
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));

  const hasFilters =
    hasSearch || typeFilter !== "" || statusFilter !== "";

  const { data, isLoading } = useQuery({
    queryKey: [
      "inventory-documents",
      page,
      typeFilter,
      statusFilter,
      debouncedSearch,
    ],
    queryFn: () =>
      fetchInventoryDocuments(
        typeFilter === "" ? undefined : typeFilter,
        statusFilter === "" ? undefined : statusFilter,
        page,
        20,
        debouncedSearch || undefined
      ),
  });

  const clearFilters = () => {
    resetSearch();
    setTypeFilter("");
    setStatusFilter("");
    setPage(1);
  };

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
            <Link href="/inventory-documents/new/transfer" className="btn-secondary">
              + {t("createTransfer")}
            </Link>
            <Link
              href="/inventory-documents/new/stock-count"
              className="btn-secondary"
            >
              + {t("createStockCount")}
            </Link>
          </div>
        }
      />

      <ListFilterBar
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchDocument")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
        <label className="text-sm text-slate-600">
          <span className="mb-1 block">{t("type")}</span>
          <select
            className="input min-w-[160px]"
            value={typeFilter}
            onChange={(e) => {
              setTypeFilter(
                e.target.value === "" ? "" : Number(e.target.value)
              );
              setPage(1);
            }}
          >
            <option value="">{tFilters("allTypes")}</option>
            {documentTypes.map((type) => (
              <option key={type} value={type}>
                {t(`types.${docTypeKey(type)}` as "types.StockIn")}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm text-slate-600">
          <span className="mb-1 block">{tCommon("status")}</span>
          <select
            className="input min-w-[160px]"
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(
                e.target.value === "" ? "" : Number(e.target.value)
              );
              setPage(1);
            }}
          >
            <option value="">{tFilters("allStatuses")}</option>
            {documentStatuses.map((status) => (
              <option key={status} value={status}>
                {statusLabel(status, t)}
              </option>
            ))}
          </select>
        </label>
      </ListFilterBar>

      <div className="card">
        {isLoading ? (
          <TableSkeleton rows={8} cols={7} />
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
