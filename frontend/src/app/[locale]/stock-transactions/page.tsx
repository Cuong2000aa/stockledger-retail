"use client";

import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton, StatCardsSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
import {
  TransactionTypeBadge,
  transactionTypeKey,
} from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { fetchStockTransactions, fetchWarehouses } from "@/lib/api";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { formatDate, formatNumber } from "@/lib/format";
import { StockTransactionType } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { ArrowDownLeft, ArrowUpRight, History, Warehouse } from "lucide-react";
import { useMemo, useState } from "react";

function isInbound(type: StockTransactionType) {
  return [
    StockTransactionType.In,
    StockTransactionType.TransferIn,
    StockTransactionType.AdjustmentIn,
    StockTransactionType.CountAdjustmentIn,
  ].includes(type);
}

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

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    const inbound = items.filter((tx) => isInbound(tx.transactionType)).length;
    const outbound = items.length - inbound;
    const netChange = items.reduce((sum, tx) => sum + tx.quantityDelta, 0);
    return {
      total: data?.totalCount ?? 0,
      inbound,
      outbound,
      netChange,
    };
  }, [data?.items, data?.totalCount]);

  const clearFilters = () => {
    resetSearch();
    setWarehouseId("");
    setPage(1);
  };

  const typeLabel = (type: StockTransactionType) =>
    t(`types.${transactionTypeKey(type)}` as "types.In");

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      {isLoading && !data ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard label={t("stats.total")} value={formatNumber(stats.total, locale)} icon={History} accent="indigo" />
          <StatCard label={t("stats.inbound")} value={formatNumber(stats.inbound, locale)} icon={ArrowDownLeft} accent="emerald" />
          <StatCard label={t("stats.outbound")} value={formatNumber(stats.outbound, locale)} icon={ArrowUpRight} accent="rose" />
          <StatCard
            label={t("stats.netChange")}
            value={formatNumber(stats.netChange, locale)}
            icon={History}
            accent="sky"
          />
        </div>
      )}

      <ListFilterBar
        variant="enhanced"
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchTransaction")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
        <label className="text-sm text-slate-600">
          <span className="mb-1.5 flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-slate-500">
            <Warehouse className="h-3.5 w-3.5" />
            {tStocks("warehouse")}
          </span>
          <select
            className="input min-w-[220px]"
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

      <DataTableCard
        title={t("title")}
        icon={History}
        count={data?.totalCount}
        countLabel={tCommon("total")}
      >
        {isLoading ? (
          <TableSkeleton rows={8} cols={7} />
        ) : !data?.items.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <>
            <div className="table-wrap max-h-[32rem] overflow-y-auto scrollbar-thin">
              <table className="data-table">
                <thead className="sticky top-0 z-10 bg-white">
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
                  {data.items.map((tx) => (
                    <tr key={tx.id} className="hover:bg-slate-50/80">
                      <td><CodePill>{tx.transactionNo}</CodePill></td>
                      <td><CodePill>{tx.sku}</CodePill></td>
                      <td>
                        <TransactionTypeBadge
                          type={tx.transactionType}
                          label={typeLabel(tx.transactionType)}
                        />
                      </td>
                      <td
                        className={`tabular-nums font-semibold ${
                          tx.quantityDelta >= 0 ? "text-emerald-700" : "text-red-700"
                        }`}
                      >
                        {tx.quantityDelta >= 0 ? "+" : ""}
                        {formatNumber(tx.quantityDelta, locale)}
                      </td>
                      <td className="tabular-nums text-slate-600">
                        {formatNumber(tx.beforeQuantity, locale)}
                      </td>
                      <td className="tabular-nums font-medium text-slate-900">
                        {formatNumber(tx.afterQuantity, locale)}
                      </td>
                      <td className="whitespace-nowrap text-xs text-slate-500">
                        {formatDate(tx.transactionDate, locale)}
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
