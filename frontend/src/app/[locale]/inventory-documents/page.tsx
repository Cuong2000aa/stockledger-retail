"use client";

import { Link } from "@/i18n/routing";
import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton, StatCardsSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
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
import {
  ArrowLeftRight,
  ChevronRight,
  ClipboardCheck,
  FileInput,
  FileOutput,
  FileText,
  SlidersHorizontal,
} from "lucide-react";
import { useMemo, useState } from "react";
import clsx from "clsx";

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

const createActions = [
  { href: "/inventory-documents/new/stock-in", labelKey: "createStockIn", icon: FileInput, primary: true as const },
  { href: "/inventory-documents/new/stock-out", labelKey: "createStockOut", icon: FileOutput, primary: false as const },
  { href: "/inventory-documents/new/adjustment", labelKey: "createAdjustment", icon: SlidersHorizontal, primary: false as const },
  { href: "/inventory-documents/new/transfer", labelKey: "createTransfer", icon: ArrowLeftRight, primary: false as const },
  { href: "/inventory-documents/new/stock-count", labelKey: "createStockCount", icon: ClipboardCheck, primary: false as const },
] as const;

export default function InventoryDocumentsPage() {
  const t = useTranslations("documents");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [typeFilter, setTypeFilter] = useState<InventoryDocumentType | "">("");
  const [statusFilter, setStatusFilter] = useState<InventoryDocumentStatus | "">("");
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));

  const hasFilters = hasSearch || typeFilter !== "" || statusFilter !== "";

  const { data, isLoading } = useQuery({
    queryKey: ["inventory-documents", page, typeFilter, statusFilter, debouncedSearch],
    queryFn: () =>
      fetchInventoryDocuments(
        typeFilter === "" ? undefined : typeFilter,
        statusFilter === "" ? undefined : statusFilter,
        page,
        20,
        debouncedSearch || undefined
      ),
  });

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    return {
      total: data?.totalCount ?? 0,
      draft: items.filter((d) => d.status === InventoryDocumentStatus.Draft).length,
      approved: items.filter((d) => d.status === InventoryDocumentStatus.Approved).length,
    };
  }, [data?.items, data?.totalCount]);

  const clearFilters = () => {
    resetSearch();
    setTypeFilter("");
    setStatusFilter("");
    setPage(1);
  };

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      <div className="mb-6 flex flex-wrap gap-2">
        {createActions.map(({ href, labelKey, icon: Icon, primary }) => (
          <Link
            key={href}
            href={href}
            className={clsx(
              "inline-flex items-center gap-2 rounded-xl px-3 py-2 text-sm font-medium transition",
              primary
                ? "btn-primary"
                : "bg-white text-slate-700 ring-1 ring-slate-200 hover:bg-slate-50"
            )}
          >
            <Icon className="h-4 w-4" />
            {t(labelKey)}
          </Link>
        ))}
      </div>

      {isLoading && !data ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <StatCard label={t("stats.total")} value={String(stats.total)} icon={FileText} accent="indigo" />
          <StatCard label={t("stats.draft")} value={String(stats.draft)} icon={FileText} accent="amber" />
          <StatCard label={t("stats.approved")} value={String(stats.approved)} icon={FileText} accent="emerald" />
        </div>
      )}

      <ListFilterBar
        variant="enhanced"
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchDocument")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
        <label className="text-sm text-slate-600">
          <span className="mb-1.5 block text-xs font-medium uppercase tracking-wide text-slate-500">
            {t("type")}
          </span>
          <select
            className="input min-w-[160px]"
            value={typeFilter}
            onChange={(e) => {
              setTypeFilter(e.target.value === "" ? "" : Number(e.target.value));
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
          <span className="mb-1.5 block text-xs font-medium uppercase tracking-wide text-slate-500">
            {tCommon("status")}
          </span>
          <select
            className="input min-w-[160px]"
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value === "" ? "" : Number(e.target.value));
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

      <DataTableCard
        title={t("title")}
        icon={FileText}
        count={data?.totalCount}
        countLabel={tCommon("total")}
      >
        {isLoading ? (
          <TableSkeleton rows={8} cols={6} />
        ) : !data?.items.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <>
            <div className="table-wrap max-h-[32rem] overflow-y-auto scrollbar-thin">
              <table className="data-table">
                <thead className="sticky top-0 z-10 bg-white">
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
                  {data.items.map((doc) => (
                    <tr key={doc.id} className="hover:bg-slate-50/80">
                      <td><CodePill>{doc.documentNo}</CodePill></td>
                      <td className="text-sm font-medium">
                        {t(`types.${docTypeKey(doc.documentType)}` as "types.StockIn")}
                      </td>
                      <td className="text-sm">{formatDate(doc.documentDate, locale)}</td>
                      <td>
                        <DocStatusBadge status={doc.status} label={statusLabel(doc.status, t)} />
                      </td>
                      <td className="text-sm text-slate-600">{doc.referenceNo ?? "—"}</td>
                      <td>
                        <Link
                          href={`/inventory-documents/${doc.id}`}
                          className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:text-brand-700"
                        >
                          {t("detail")}
                          <ChevronRight className="h-3.5 w-3.5" />
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </DataTableCard>
    </div>
  );
}
