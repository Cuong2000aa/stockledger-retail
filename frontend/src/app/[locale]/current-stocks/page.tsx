"use client";

import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton, StatCardsSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
import { UnitBarcodesModal } from "@/components/UnitBarcodesModal";
import { useListSearch } from "@/hooks/useListSearch";
import { fetchCurrentStocks, fetchWarehouses } from "@/lib/api";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { formatDate, formatNumber } from "@/lib/format";
import type { CurrentStock } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { Boxes, Lock, Package, Warehouse } from "lucide-react";
import { useMemo, useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import clsx from "clsx";

export default function CurrentStocksPage() {
  const t = useTranslations("stocks");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const locale = useLocale();
  const searchParams = useSearchParams();
  const [page, setPage] = useState(1);
  const [warehouseId, setWarehouseId] = useState("");
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));
  const [barcodeModalStock, setBarcodeModalStock] = useState<CurrentStock | null>(
    null
  );

  useEffect(() => {
    const initialSearch = searchParams.get("search");
    const initialWarehouseId = searchParams.get("warehouseId");
    if (initialSearch) setSearch(initialSearch);
    if (initialWarehouseId) setWarehouseId(initialWarehouseId);
  }, [searchParams, setSearch]);

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

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    return {
      total: data?.totalCount ?? 0,
      onHand: items.reduce((sum, s) => sum + s.quantityOnHand, 0),
      reserved: items.reduce((sum, s) => sum + s.quantityReserved, 0),
      available: items.reduce((sum, s) => sum + s.quantityAvailable, 0),
    };
  }, [data?.items, data?.totalCount]);

  const clearFilters = () => {
    resetSearch();
    setWarehouseId("");
    setPage(1);
  };

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      {isLoading && !data ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard label={t("stats.total")} value={formatNumber(stats.total, locale)} icon={Boxes} accent="indigo" />
          <StatCard label={t("onHand")} value={formatNumber(stats.onHand, locale)} icon={Package} accent="emerald" />
          <StatCard label={t("reserved")} value={formatNumber(stats.reserved, locale)} icon={Lock} accent="amber" />
          <StatCard label={t("available")} value={formatNumber(stats.available, locale)} icon={Warehouse} accent="sky" />
        </div>
      )}

      <ListFilterBar
        variant="enhanced"
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchStock")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
        <label className="text-sm text-slate-600">
          <span className="mb-1.5 flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-slate-500">
            <Warehouse className="h-3.5 w-3.5" />
            {t("warehouse")}
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
        icon={Boxes}
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
                    <th>{t("sku")}</th>
                    <th>{t("warehouse")}</th>
                    <th>{t("onHand")}</th>
                    <th>{t("reserved")}</th>
                    <th>{t("available")}</th>
                    <th>{t("lastUpdated")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((s) => {
                    const lowStock = s.quantityAvailable <= 0;
                    return (
                      <tr
                        key={s.id}
                        className={clsx("hover:bg-slate-50/80", lowStock && "bg-red-50/40")}
                      >
                        <td><CodePill>{s.sku}</CodePill></td>
                        <td>
                          <div className="text-sm font-medium text-slate-900">{s.warehouseName}</div>
                          <div className="text-xs text-slate-500">{s.warehouseCode}</div>
                        </td>
                        <td className="tabular-nums font-medium">{formatNumber(s.quantityOnHand, locale)}</td>
                        <td className="tabular-nums text-amber-700">{formatNumber(s.quantityReserved, locale)}</td>
                        <td
                          className={clsx(
                            "tabular-nums font-semibold",
                            lowStock ? "text-red-700" : "text-emerald-700"
                          )}
                        >
                          {formatNumber(s.quantityAvailable, locale)}
                        </td>
                        <td className="whitespace-nowrap text-xs text-slate-500">
                          {formatDate(s.lastUpdatedAt, locale)}
                        </td>
                        <td>
                          {s.isBarcode ? (
                            <button
                              type="button"
                              className="text-sm font-medium text-brand-600 hover:text-brand-700"
                              onClick={() => setBarcodeModalStock(s)}
                            >
                              {t("viewBarcodes")}
                            </button>
                          ) : (
                            <span className="text-xs text-slate-400">—</span>
                          )}
                        </td>
                      </tr>
                    );
                  })}
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

      {barcodeModalStock && (
        <UnitBarcodesModal
          open={!!barcodeModalStock}
          onClose={() => setBarcodeModalStock(null)}
          productVariantId={barcodeModalStock.productVariantId}
          warehouseId={barcodeModalStock.warehouseId}
          sku={barcodeModalStock.sku}
          warehouseName={barcodeModalStock.warehouseName}
        />
      )}
    </div>
  );
}
