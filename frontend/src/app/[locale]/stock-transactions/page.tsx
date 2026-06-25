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
import {
  expandStockTransactionList,
  isInboundTransaction,
} from "@/lib/stockTransactionDisplay";
import { StockTransactionType } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/routing";
import { ArrowDownLeft, ArrowUpRight, History, Warehouse } from "lucide-react";
import clsx from "clsx";
import { useMemo, useState } from "react";

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

  const displayRows = useMemo(
    () => expandStockTransactionList(data?.items ?? []),
    [data?.items]
  );

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    const inbound = items.filter((tx) => isInboundTransaction(tx.transactionType)).length;
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
          <TableSkeleton rows={8} cols={9} />
        ) : !displayRows.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <>
            <div className="table-wrap max-h-[36rem] overflow-y-auto scrollbar-thin">
              <table className="data-table text-sm">
                <thead className="sticky top-0 z-10 bg-white shadow-sm">
                  <tr>
                    <th>{t("type")}</th>
                    <th>{t("documentNo")}</th>
                    <th>{t("sku")}</th>
                    <th>{t("sourceWarehouse")}</th>
                    <th>{t("destinationWarehouse")}</th>
                    <th className="text-right">{t("delta")}</th>
                    <th>{t("barcodes")}</th>
                    <th>{t("createdBy")}</th>
                    <th>{t("date")}</th>
                  </tr>
                </thead>
                <tbody>
                  {displayRows.map((row, index) => {
                    const prev = displayRows[index - 1];
                    const sameGroup = prev?.transactionId === row.transactionId;
                    return (
                      <tr
                        key={row.rowKey}
                        className={clsx(
                          "hover:bg-slate-50/80",
                          sameGroup && "border-t border-dashed border-slate-100",
                          row.isSplitLine && sameGroup && "bg-slate-50/30"
                        )}
                      >
                        <td>
                          <TransactionTypeBadge
                            type={row.transactionType}
                            label={typeLabel(row.transactionType)}
                          />
                        </td>
                        <td>
                          {row.documentNo ? (
                            <Link
                              href={`/inventory-documents/${row.documentId}`}
                              className="font-mono text-xs font-medium text-brand-600 hover:underline"
                            >
                              {row.documentNo}
                            </Link>
                          ) : (
                            <span className="text-slate-300">—</span>
                          )}
                        </td>
                        <td>
                          <CodePill>{row.sku}</CodePill>
                        </td>
                        <td className="text-xs font-medium text-slate-700">
                          {row.sourceWarehouse}
                        </td>
                        <td className="text-xs font-medium text-slate-700">
                          {row.destinationWarehouse}
                        </td>
                        <td
                          className={clsx(
                            "text-right tabular-nums font-semibold",
                            row.quantityDelta >= 0 ? "text-emerald-700" : "text-red-700"
                          )}
                        >
                          {row.quantityDelta >= 0 ? "+" : ""}
                          {formatNumber(row.quantityDelta, locale)}
                        </td>
                        <td className="font-mono text-xs text-slate-800">
                          {row.barcode ?? "—"}
                        </td>
                        <td className="text-xs text-slate-600">
                          {row.createdBy ?? "—"}
                        </td>
                        <td className="whitespace-nowrap text-xs text-slate-500">
                          {formatDate(row.transactionDate, locale)}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <Pagination
              page={data!.page}
              pageSize={data!.pageSize}
              totalCount={data!.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </DataTableCard>
    </div>
  );
}
