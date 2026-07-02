"use client";

import { useWarehouseScope } from "@/hooks/useWarehouseScope";
import { fetchBrands } from "@/features/admin/api";
import { fetchWarehouses } from "@/lib/api";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { useQuery } from "@tanstack/react-query";
import clsx from "clsx";
import { Calendar, Filter, RotateCcw, Tag, Warehouse } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import type { InsightTab } from "../types";

type InsightFiltersProps = {
  activeTab: InsightTab;
  warehouseId: string;
  onWarehouseChange: (id: string) => void;
  brandId: string;
  onBrandChange: (id: string) => void;
  daysWithoutOutbound: number;
  onDaysWithoutOutboundChange: (days: number) => void;
  lookbackDays: number;
  onLookbackDaysChange: (days: number) => void;
  onReset: () => void;
};

export function InsightFilters({
  activeTab,
  warehouseId,
  onWarehouseChange,
  brandId,
  onBrandChange,
  daysWithoutOutbound,
  onDaysWithoutOutboundChange,
  lookbackDays,
  onLookbackDaysChange,
  onReset,
}: InsightFiltersProps) {
  const t = useTranslations("insights");
  const tFilters = useTranslations("filters");
  const { canSelectAllWarehouses } = useWarehouseScope();
  const [warehouseSearch, setWarehouseSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(warehouseSearch.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [warehouseSearch]);

  const { data: warehouses, isFetching } = useQuery({
    queryKey: ["warehouses-insights", debouncedSearch],
    queryFn: () => fetchWarehouses(1, 100, debouncedSearch || undefined),
    staleTime: 5 * 60_000,
  });

  const { data: brands } = useQuery({
    queryKey: ["brands-insights"],
    queryFn: fetchBrands,
    staleTime: 5 * 60_000,
  });

  const showDeadDays =
    activeTab === "deadStock" ||
    activeTab === "markdown" ||
    activeTab === "seasonClearance";
  const showLookback =
    activeTab === "velocity" ||
    activeTab === "transfer" ||
    activeTab === "promotionRisk" ||
    activeTab === "reorderRisk" ||
    activeTab === "trend" ||
    activeTab === "brokenSize" ||
    activeTab === "seasonClearance";

  return (
    <div className="border-b border-slate-100 bg-gradient-to-br from-slate-50 via-white to-violet-50/30 px-5 py-4">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2 text-sm font-semibold text-slate-700">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-500/10 text-brand-600 ring-1 ring-brand-500/20">
            <Filter className="h-4 w-4" />
          </span>
          {t("filters.title")}
        </div>
        <button
          type="button"
          className="inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-xs font-medium text-slate-500 transition hover:bg-white hover:text-slate-800 hover:ring-1 hover:ring-slate-200"
          onClick={onReset}
        >
          <RotateCcw className="h-3.5 w-3.5" />
          {tFilters("clear")}
        </button>
      </div>

      <div
        className={clsx(
          "grid gap-4",
          showDeadDays || showLookback ? "md:grid-cols-3" : "md:grid-cols-2"
        )}
      >
        <label className="text-sm text-slate-600">
          <span className="mb-1.5 flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-slate-500">
            <Tag className="h-3.5 w-3.5" />
            {t("filters.brand")}
          </span>
          <select className="input" value={brandId} onChange={(e) => onBrandChange(e.target.value)}>
            <option value="">{t("filters.allBrands")}</option>
            {brands?.map((brand) => (
              <option key={brand.id} value={brand.id}>
                {brand.name}
              </option>
            ))}
          </select>
        </label>

        <label className="text-sm text-slate-600">
          <span className="mb-1.5 flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-slate-500">
            <Warehouse className="h-3.5 w-3.5" />
            {t("filters.warehouse")}
          </span>
          <div className="space-y-1.5">
            <input
              type="search"
              className="input"
              placeholder={tFilters("searchWarehouse")}
              value={warehouseSearch}
              onChange={(e) => setWarehouseSearch(e.target.value)}
            />
            <select
              className="input"
              value={warehouseId}
              onChange={(e) => onWarehouseChange(e.target.value)}
            >
              {canSelectAllWarehouses ? (
                <option value="">{t("filters.allWarehouses")}</option>
              ) : null}
              {warehouses?.items.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {formatWarehouseOptionLabel(warehouse)}
                </option>
              ))}
            </select>
            {isFetching && debouncedSearch ? (
              <p className="text-[11px] text-slate-400">{t("filters.searching")}</p>
            ) : null}
          </div>
        </label>

        {showDeadDays ? (
          <label className="text-sm text-slate-600">
            <span className="mb-1.5 flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-slate-500">
              <Calendar className="h-3.5 w-3.5" />
              {t("filters.deadStockDays")}
            </span>
            <input
              type="number"
              min={1}
              className="input"
              value={daysWithoutOutbound}
              onChange={(e) => onDaysWithoutOutboundChange(Number(e.target.value) || 1)}
            />
            <p className="mt-1 text-[11px] text-slate-400">{t("filters.deadStockDaysHint")}</p>
          </label>
        ) : null}

        {showLookback ? (
          <label className="text-sm text-slate-600">
            <span className="mb-1.5 flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-slate-500">
              <Calendar className="h-3.5 w-3.5" />
              {t("filters.lookbackDays")}
            </span>
            <input
              type="number"
              min={1}
              className="input"
              value={lookbackDays}
              onChange={(e) => onLookbackDaysChange(Number(e.target.value) || 1)}
            />
            <p className="mt-1 text-[11px] text-slate-400">{t("filters.lookbackDaysHint")}</p>
          </label>
        ) : null}
      </div>
    </div>
  );
}
