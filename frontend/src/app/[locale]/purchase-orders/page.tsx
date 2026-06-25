"use client";

import { Link } from "@/i18n/routing";
import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton, StatCardsSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
import { PoStatusBadge } from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { fetchPurchaseOrders, fetchSuppliers } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { purchaseOrderStatusLabel } from "@/lib/purchaseOrderStatus";
import { PurchaseOrderStatus } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { ChevronRight, ClipboardList, PackageCheck, Plus, ShoppingCart } from "lucide-react";
import { useMemo, useState } from "react";

export default function PurchaseOrdersPage() {
  const t = useTranslations("purchaseOrders");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<PurchaseOrderStatus | "">("");
  const [supplierId, setSupplierId] = useState("");
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));

  const { data: suppliers } = useQuery({
    queryKey: ["suppliers-all"],
    queryFn: () => fetchSuppliers(1, 200),
  });

  const hasFilters = hasSearch || statusFilter !== "" || supplierId !== "";

  const { data, isLoading } = useQuery({
    queryKey: ["purchase-orders", page, statusFilter, supplierId, debouncedSearch],
    queryFn: () =>
      fetchPurchaseOrders(
        statusFilter === "" ? undefined : statusFilter,
        supplierId || undefined,
        page,
        20,
        debouncedSearch || undefined
      ),
  });

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    return {
      total: data?.totalCount ?? 0,
      open: items.filter(
        (po) =>
          po.status === PurchaseOrderStatus.Draft ||
          po.status === PurchaseOrderStatus.Submitted ||
          po.status === PurchaseOrderStatus.PendingApproval ||
          po.status === PurchaseOrderStatus.PartiallyReceived
      ).length,
      received: items.filter((po) => po.status === PurchaseOrderStatus.Received).length,
    };
  }, [data?.items, data?.totalCount]);

  const clearFilters = () => {
    resetSearch();
    setStatusFilter("");
    setSupplierId("");
    setPage(1);
  };

  return (
    <div>
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <Link href="/purchase-orders/new" className="btn-primary">
            <Plus className="h-4 w-4" />
            {t("create")}
          </Link>
        }
      />

      {isLoading && !data ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <StatCard label={t("stats.total")} value={String(stats.total)} icon={ShoppingCart} accent="indigo" />
          <StatCard label={t("stats.open")} value={String(stats.open)} icon={ClipboardList} accent="amber" />
          <StatCard label={t("stats.received")} value={String(stats.received)} icon={PackageCheck} accent="emerald" />
        </div>
      )}

      <ListFilterBar
        variant="enhanced"
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchPo")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
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
            {Object.values(PurchaseOrderStatus)
              .filter((v) => typeof v === "number")
              .map((s) => (
                <option key={s} value={s}>
                  {purchaseOrderStatusLabel(s as PurchaseOrderStatus, t)}
                </option>
              ))}
          </select>
        </label>
        <label className="text-sm text-slate-600">
          <span className="mb-1.5 block text-xs font-medium uppercase tracking-wide text-slate-500">
            {t("supplier")}
          </span>
          <select
            className="input min-w-[220px]"
            value={supplierId}
            onChange={(e) => {
              setSupplierId(e.target.value);
              setPage(1);
            }}
          >
            <option value="">{tFilters("allSuppliers")}</option>
            {suppliers?.items.map((s) => (
              <option key={s.id} value={s.id}>
                {s.code} — {s.name}
              </option>
            ))}
          </select>
        </label>
      </ListFilterBar>

      <DataTableCard
        title={t("title")}
        icon={ShoppingCart}
        count={data?.totalCount}
        countLabel={tCommon("total")}
      >
        {isLoading ? (
          <TableSkeleton rows={8} cols={5} />
        ) : !data?.items.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <>
            <div className="table-wrap max-h-[32rem] overflow-y-auto scrollbar-thin">
              <table className="data-table">
                <thead className="sticky top-0 z-10 bg-white">
                  <tr>
                    <th>{t("poNo")}</th>
                    <th>{t("supplier")}</th>
                    <th>{t("orderDate")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((po) => (
                    <tr key={po.id} className="hover:bg-slate-50/80">
                      <td><CodePill>{po.poNo}</CodePill></td>
                      <td>
                        <div className="font-medium text-slate-900">{po.supplierName}</div>
                        <div className="text-xs text-slate-500">{po.supplierCode}</div>
                      </td>
                      <td className="text-sm">{formatDate(po.orderDate, locale)}</td>
                      <td>
                        <PoStatusBadge status={po.status} label={purchaseOrderStatusLabel(po.status, t)} />
                      </td>
                      <td>
                        <Link
                          href={`/purchase-orders/${po.id}`}
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
