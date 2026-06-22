"use client";

import { ListFilterBar } from "@/components/ListFilterBar";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { useListSearch } from "@/hooks/useListSearch";
import { fetchCurrentStocks, fetchWarehouses } from "@/lib/api";
import { formatDate, formatNumber } from "@/lib/format";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

export default function CurrentStocksPage() {
  const t = useTranslations("stocks");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [warehouseId, setWarehouseId] = useState("");
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-all"],
    queryFn: () => fetchWarehouses(1, 100),
  });

  const hasFilters = hasSearch || warehouseId !== "";

  const { data, isLoading } = useQuery({
    queryKey: ["current-stocks", page, warehouseId, debouncedSearch],
    queryFn: () =>
      fetchCurrentStocks(
        warehouseId || undefined,
        undefined,
        page,
        20,
        debouncedSearch || undefined
      ),
  });

  const clearFilters = () => {
    resetSearch();
    setWarehouseId("");
    setPage(1);
  };

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      <ListFilterBar
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchStock")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
        <label className="text-sm text-slate-600">
          <span className="mb-1 block">{t("warehouse")}</span>
          <select
            className="input min-w-[200px]"
            value={warehouseId}
            onChange={(e) => {
              setWarehouseId(e.target.value);
              setPage(1);
            }}
          >
            <option value="">{tFilters("allWarehouses")}</option>
            {warehouses?.items.map((w) => (
              <option key={w.id} value={w.id}>
                {w.code} — {w.name}
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
                    <th>{t("sku")}</th>
                    <th>{t("warehouse")}</th>
                    <th>{t("onHand")}</th>
                    <th>{t("reserved")}</th>
                    <th>{t("available")}</th>
                    <th>{t("lastUpdated")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.map((s) => (
                    <tr key={s.id}>
                      <td className="font-mono text-xs">{s.sku}</td>
                      <td>
                        {s.warehouseCode} — {s.warehouseName}
                      </td>
                      <td>{formatNumber(s.quantityOnHand, locale)}</td>
                      <td>{formatNumber(s.quantityReserved, locale)}</td>
                      <td>{formatNumber(s.quantityAvailable, locale)}</td>
                      <td className="text-xs">
                        {formatDate(s.lastUpdatedAt, locale)}
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
