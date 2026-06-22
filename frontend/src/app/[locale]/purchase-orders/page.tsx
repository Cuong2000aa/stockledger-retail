"use client";

import { Link } from "@/i18n/routing";
import { ListFilterBar } from "@/components/ListFilterBar";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { useListSearch } from "@/hooks/useListSearch";
import { fetchPurchaseOrders, fetchSuppliers } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { PurchaseOrderStatus } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

function poStatusLabel(
  status: PurchaseOrderStatus,
  t: ReturnType<typeof useTranslations<"purchaseOrders">>
) {
  switch (status) {
    case PurchaseOrderStatus.Draft:
      return t("statusDraft");
    case PurchaseOrderStatus.Submitted:
      return t("statusSubmitted");
    case PurchaseOrderStatus.PartiallyReceived:
      return t("statusPartiallyReceived");
    case PurchaseOrderStatus.Received:
      return t("statusReceived");
    case PurchaseOrderStatus.Cancelled:
      return t("statusCancelled");
    default:
      return String(status);
  }
}

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
    queryKey: [
      "purchase-orders",
      page,
      statusFilter,
      supplierId,
      debouncedSearch,
    ],
    queryFn: () =>
      fetchPurchaseOrders(
        statusFilter === "" ? undefined : statusFilter,
        supplierId || undefined,
        page,
        20,
        debouncedSearch || undefined
      ),
  });

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
            + {t("create")}
          </Link>
        }
      />

      <ListFilterBar
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchPo")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
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
            {Object.values(PurchaseOrderStatus)
              .filter((v) => typeof v === "number")
              .map((s) => (
                <option key={s} value={s}>
                  {poStatusLabel(s as PurchaseOrderStatus, t)}
                </option>
              ))}
          </select>
        </label>
        <label className="text-sm text-slate-600">
          <span className="mb-1 block">{t("supplier")}</span>
          <select
            className="input min-w-[200px]"
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

      <div className="card">
        {isLoading ? (
          <p className="p-6 text-slate-500">{tCommon("loading")}</p>
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>{t("poNo")}</th>
                    <th>{t("supplier")}</th>
                    <th>{t("orderDate")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.map((po) => (
                    <tr key={po.id}>
                      <td className="font-mono text-xs">{po.poNo}</td>
                      <td>
                        {po.supplierCode} — {po.supplierName}
                      </td>
                      <td>{formatDate(po.orderDate, locale)}</td>
                      <td>{poStatusLabel(po.status, t)}</td>
                      <td>
                        <Link
                          href={`/purchase-orders/${po.id}`}
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
