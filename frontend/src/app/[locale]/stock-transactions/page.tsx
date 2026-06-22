"use client";

import { ListFilterBar } from "@/components/ListFilterBar";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { transactionTypeKey } from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { fetchStockTransactions, fetchWarehouses } from "@/lib/api";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { formatDate, formatNumber } from "@/lib/format";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

export default function StockTransactionsPage() {
  const t = useTranslations("transactions");
  const tStocks = useTranslations("stocks");
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
    queryKey: ["stock-transactions", page, warehouseId, debouncedSearch],
    queryFn: () =>
      fetchStockTransactions(
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
        searchPlaceholder={tFilters("searchTransaction")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
        <label className="text-sm text-slate-600">
          <span className="mb-1 block">{tStocks("warehouse")}</span>
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
                {formatWarehouseOptionLabel(w)}
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
                    <th>{t("transactionNo")}</th>
                    <th>{t("sku")}</th>
                    <th>{t("type")}</th>
                    <th>{t("delta")}</th>
                    <th>{t("before")}</th>
                    <th>{t("after")}</th>
                    <th>{t("date")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.map((tx) => (
                    <tr key={tx.id}>
                      <td className="font-mono text-xs">{tx.transactionNo}</td>
                      <td className="font-mono text-xs">{tx.sku}</td>
                      <td>{transactionTypeKey(tx.transactionType)}</td>
                      <td
                        className={
                          tx.quantityDelta >= 0
                            ? "text-green-700"
                            : "text-red-700"
                        }
                      >
                        {formatNumber(tx.quantityDelta, locale)}
                      </td>
                      <td>{formatNumber(tx.beforeQuantity, locale)}</td>
                      <td>{formatNumber(tx.afterQuantity, locale)}</td>
                      <td className="text-xs">
                        {formatDate(tx.transactionDate, locale)}
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
