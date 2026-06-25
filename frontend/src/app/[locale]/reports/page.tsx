"use client";

import { DataTableCard, EmptyTableState } from "@/components/DataTableCard";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
import {
  fetchInventoryValueReport,
  fetchNearExpiryLots,
  fetchNxtReport,
  runStockReconciliation,
} from "@/features/reports/api";
import { fetchWarehouses } from "@/lib/api";
import { formatDateOnly, formatNumber } from "@/lib/format";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import {
  AlertTriangle,
  ArrowDownToLine,
  ArrowUpFromLine,
  BarChart3,
  Calendar,
  Filter,
  Package,
  RefreshCw,
  Search,
} from "lucide-react";
import { useMemo, useState } from "react";
import { useNotify } from "@/hooks/useNotify";
import clsx from "clsx";

export default function ReportsPage() {
  const t = useTranslations("reports");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const { notifyError, notifySuccess } = useNotify();
  const [warehouseId, setWarehouseId] = useState("");
  const [warehouseSearch, setWarehouseSearch] = useState("");
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date();
    d.setDate(1);
    return d.toISOString().slice(0, 10);
  });
  const [toDate, setToDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [nxtPage, setNxtPage] = useState(1);
  const [loadNxt, setLoadNxt] = useState(false);
  const [loadExpiry, setLoadExpiry] = useState(false);

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-report", warehouseSearch],
    queryFn: () => fetchWarehouses(1, 100, warehouseSearch || undefined),
    staleTime: 5 * 60_000,
  });

  const { data: valueReport, isLoading: valueLoading, isError: valueError } = useQuery({
    queryKey: ["report-inventory-value", warehouseId],
    queryFn: () => fetchInventoryValueReport(warehouseId || undefined, undefined, 1, 1),
    staleTime: 2 * 60_000,
  });

  const { data: nxtReport, isLoading: nxtLoading, isError: nxtError } = useQuery({
    queryKey: ["report-nxt", fromDate, toDate, warehouseId, nxtPage],
    queryFn: () => fetchNxtReport(fromDate, toDate, warehouseId || undefined, nxtPage, 50),
    enabled: loadNxt,
    staleTime: 2 * 60_000,
  });

  const { data: nearExpiry, isLoading: expiryLoading } = useQuery({
    queryKey: ["report-near-expiry", warehouseId],
    queryFn: () => fetchNearExpiryLots(30, warehouseId || undefined),
    enabled: loadExpiry,
    staleTime: 5 * 60_000,
  });

  const reconcileMutation = useMutation({
    mutationFn: runStockReconciliation,
    onSuccess: () => notifySuccess(t("reconciliationDone")),
    onError: notifyError,
  });

  const expiryCritical = useMemo(
    () => (nearExpiry ?? []).filter((x) => x.daysUntilExpiry <= 7).length,
    [nearExpiry]
  );

  const periodLabel = `${formatDateOnly(fromDate, locale)} – ${formatDateOnly(toDate, locale)}`;

  const loadAllReports = () => {
    setNxtPage(1);
    setLoadNxt(true);
    setLoadExpiry(true);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <button
            type="button"
            className="btn-secondary"
            disabled={reconcileMutation.isPending}
            onClick={() => reconcileMutation.mutate()}
          >
            <RefreshCw className={clsx("h-4 w-4", reconcileMutation.isPending && "animate-spin")} />
            {t("runReconciliation")}
          </button>
        }
      />

      <div className="card p-5">
        <div className="mb-4 flex items-center gap-2 text-sm font-medium text-slate-700">
          <Filter className="h-4 w-4 text-brand-600" />
          {t("filtersTitle")}
        </div>
        <div className="grid gap-4 lg:grid-cols-[1fr_auto] lg:items-end">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-slate-500">{t("warehouse")}</span>
              <div className="relative">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  type="search"
                  className="input-field pl-9"
                  placeholder={t("searchWarehouse")}
                  value={warehouseSearch}
                  onChange={(e) => setWarehouseSearch(e.target.value)}
                />
              </div>
              <select
                className="input-field w-full"
                value={warehouseId}
                onChange={(e) => {
                  setWarehouseId(e.target.value);
                  setNxtPage(1);
                }}
              >
                <option value="">{t("allWarehouses")}</option>
                {warehouses?.items.map((w) => (
                  <option key={w.id} value={w.id}>
                    {formatWarehouseOptionLabel(w)}
                  </option>
                ))}
              </select>
            </label>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-slate-500">{t("fromDate")}</span>
              <input
                type="date"
                className="input-field w-full"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
              />
            </label>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-slate-500">{t("toDate")}</span>
              <input
                type="date"
                className="input-field w-full"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
              />
            </label>
            <div className="flex flex-col gap-2 sm:col-span-2 lg:col-span-1">
              <button type="button" className="btn-primary w-full" onClick={loadAllReports}>
                {t("loadAll")}
              </button>
              <div className="flex gap-2">
                <button
                  type="button"
                  className="btn-secondary flex-1 text-xs"
                  onClick={() => {
                    setNxtPage(1);
                    setLoadNxt(true);
                  }}
                >
                  {t("loadNxt")}
                </button>
                <button
                  type="button"
                  className="btn-secondary flex-1 text-xs"
                  onClick={() => setLoadExpiry(true)}
                >
                  {t("loadExpiry")}
                </button>
              </div>
            </div>
          </div>
        </div>
        {loadNxt && (
          <p className="mt-4 text-xs text-slate-500">
            {t("period")}: <span className="font-medium text-slate-700">{periodLabel}</span>
          </p>
        )}
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label={t("inventoryValue")}
          value={
            valueLoading ? "…" : valueError ? "—" : formatNumber(valueReport?.totalValue ?? 0, locale)
          }
          icon={Package}
          accent="indigo"
        />
        <StatCard
          label={t("nxtClosing")}
          value={formatNumber(nxtReport?.totalClosingValue ?? 0, locale)}
          icon={BarChart3}
          accent="sky"
        />
        <StatCard
          label={t("nearExpiry")}
          value={String(nearExpiry?.length ?? 0)}
          icon={Calendar}
          accent="amber"
        />
        <StatCard
          label={t("expiryCritical")}
          value={String(expiryCritical)}
          icon={AlertTriangle}
          accent="rose"
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <DataTableCard title={t("nxtTitle")} icon={BarChart3}>
          {!loadNxt ? (
            <p className="p-6 text-center text-sm text-slate-500">{t("clickLoadNxt")}</p>
          ) : nxtLoading ? (
            <p className="p-6 text-center text-sm text-slate-500">{tCommon("loading")}</p>
          ) : nxtError ? (
            <p className="p-6 text-center text-sm text-red-600">{tCommon("loadError")}</p>
          ) : !nxtReport?.lines.length ? (
            <EmptyTableState message={tCommon("noData")} />
          ) : (
            <>
              <div className="border-b border-slate-100 bg-slate-50/80 px-4 py-3">
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  {t("nxtSummary")}
                </p>
                <div className="mt-2 grid grid-cols-3 gap-3 text-sm">
                  <div>
                    <span className="flex items-center gap-1 text-emerald-700">
                      <ArrowDownToLine className="h-3.5 w-3.5" />
                      {t("totalIn")}
                    </span>
                    <p className="font-semibold text-slate-900">
                      {formatNumber(nxtReport.totalInValue, locale)}
                    </p>
                  </div>
                  <div>
                    <span className="flex items-center gap-1 text-rose-700">
                      <ArrowUpFromLine className="h-3.5 w-3.5" />
                      {t("totalOut")}
                    </span>
                    <p className="font-semibold text-slate-900">
                      {formatNumber(nxtReport.totalOutValue, locale)}
                    </p>
                  </div>
                  <div>
                    <span className="text-slate-500">{t("closing")}</span>
                    <p className="font-semibold text-slate-900">
                      {formatNumber(nxtReport.totalClosingValue, locale)}
                    </p>
                  </div>
                </div>
                <p className="mt-2 text-xs text-slate-500">
                  {t("linesCount", { count: nxtReport.totalLineCount })}
                </p>
              </div>
              <div className="table-wrap max-h-[28rem] overflow-auto">
                <table className="data-table text-sm">
                  <thead className="sticky top-0 z-10 bg-white shadow-sm">
                    <tr>
                      <th>SKU</th>
                      <th>{t("warehouseCol")}</th>
                      <th className="text-right">{t("opening")}</th>
                      <th className="text-right">{t("in")}</th>
                      <th className="text-right">{t("out")}</th>
                      <th className="text-right">{t("closing")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {nxtReport.lines.map((line) => (
                      <tr key={`${line.productVariantId}-${line.warehouseId}`}>
                        <td className="font-mono text-xs">{line.sku}</td>
                        <td className="text-xs text-slate-600">{line.warehouseCode}</td>
                        <td className="text-right tabular-nums">
                          {formatNumber(line.openingQuantity, locale)}
                        </td>
                        <td
                          className={clsx(
                            "text-right tabular-nums",
                            line.inQuantity > 0 && "font-medium text-emerald-700"
                          )}
                        >
                          {formatNumber(line.inQuantity, locale)}
                        </td>
                        <td
                          className={clsx(
                            "text-right tabular-nums",
                            line.outQuantity > 0 && "font-medium text-rose-700"
                          )}
                        >
                          {formatNumber(line.outQuantity, locale)}
                        </td>
                        <td className="text-right font-medium tabular-nums text-slate-900">
                          {formatNumber(line.closingQuantity, locale)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {nxtReport.totalLineCount > nxtReport.pageSize ? (
                <Pagination
                  page={nxtPage}
                  pageSize={nxtReport.pageSize}
                  totalCount={nxtReport.totalLineCount}
                  onChange={setNxtPage}
                />
              ) : null}
            </>
          )}
        </DataTableCard>

        <DataTableCard title={t("nearExpiryTitle")} icon={Calendar}>
          {!loadExpiry ? (
            <p className="p-6 text-center text-sm text-slate-500">{t("clickLoadExpiry")}</p>
          ) : expiryLoading ? (
            <p className="p-6 text-center text-sm text-slate-500">{tCommon("loading")}</p>
          ) : !nearExpiry?.length ? (
            <EmptyTableState message={t("noNearExpiry")} />
          ) : (
            <div className="table-wrap max-h-[28rem] overflow-auto">
              <table className="data-table text-sm">
                <thead className="sticky top-0 z-10 bg-white shadow-sm">
                  <tr>
                    <th>{t("lot")}</th>
                    <th>SKU</th>
                    <th>{t("warehouseCol")}</th>
                    <th className="text-right">{t("qty")}</th>
                    <th>HSD</th>
                    <th className="text-right">{t("daysLeft")}</th>
                  </tr>
                </thead>
                <tbody>
                  {nearExpiry.map((lot) => (
                    <tr key={`${lot.stockLotId}-${lot.warehouseId}`}>
                      <td className="font-mono text-xs">{lot.lotCode}</td>
                      <td className="font-mono text-xs">{lot.sku}</td>
                      <td className="text-xs text-slate-600">{lot.warehouseCode}</td>
                      <td className="text-right tabular-nums">
                        {formatNumber(lot.quantityOnHand, locale)}
                      </td>
                      <td>{formatDateOnly(lot.expiryDate, locale)}</td>
                      <td className="text-right">
                        {lot.daysUntilExpiry <= 7 ? (
                          <span className="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800">
                            {lot.daysUntilExpiry} · {t("criticalBadge")}
                          </span>
                        ) : lot.daysUntilExpiry <= 14 ? (
                          <span className="inline-flex items-center rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-900">
                            {lot.daysUntilExpiry} · {t("warningBadge")}
                          </span>
                        ) : (
                          <span className="tabular-nums text-slate-700">{lot.daysUntilExpiry}</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </DataTableCard>
      </div>
    </div>
  );
}
