"use client";

import { DataTableCard, EmptyTableState } from "@/components/DataTableCard";
import { PageHeader } from "@/components/PageHeader";
import { StatCard } from "@/components/StatCard";
import {
  fetchInventoryValueReport,
  fetchNearExpiryLots,
  fetchNxtReport,
  runStockReconciliation,
} from "@/features/reports/api";
import { fetchWarehouses } from "@/lib/api";
import { formatDate, formatNumber } from "@/lib/format";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { BarChart3, Calendar, Package, RefreshCw } from "lucide-react";
import { useMemo, useState } from "react";
import { useNotify } from "@/hooks/useNotify";

export default function ReportsPage() {
  const t = useTranslations("reports");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const { notifyError, notifySuccess } = useNotify();
  const [warehouseId, setWarehouseId] = useState("");
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date();
    d.setDate(1);
    return d.toISOString().slice(0, 10);
  });
  const [toDate, setToDate] = useState(() => new Date().toISOString().slice(0, 10));

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-all"],
    queryFn: () => fetchWarehouses(1, 200),
  });

  const { data: valueReport, isLoading: valueLoading } = useQuery({
    queryKey: ["report-inventory-value", warehouseId],
    queryFn: () => fetchInventoryValueReport(warehouseId || undefined),
  });

  const { data: nxtReport, isLoading: nxtLoading } = useQuery({
    queryKey: ["report-nxt", fromDate, toDate, warehouseId],
    queryFn: () => fetchNxtReport(fromDate, toDate, warehouseId || undefined),
  });

  const { data: nearExpiry } = useQuery({
    queryKey: ["report-near-expiry", warehouseId],
    queryFn: () => fetchNearExpiryLots(30, warehouseId || undefined),
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

  return (
    <div>
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
            <RefreshCw className="h-4 w-4" />
            {t("runReconciliation")}
          </button>
        }
      />

      <div className="mb-4 flex flex-wrap gap-3">
        <select
          className="input-field min-w-[200px]"
          value={warehouseId}
          onChange={(e) => setWarehouseId(e.target.value)}
        >
          <option value="">{t("allWarehouses")}</option>
          {warehouses?.items.map((w) => (
            <option key={w.id} value={w.id}>
              {w.code} — {w.name}
            </option>
          ))}
        </select>
        <input type="date" className="input-field" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        <input type="date" className="input-field" value={toDate} onChange={(e) => setToDate(e.target.value)} />
      </div>

      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label={t("inventoryValue")}
          value={formatNumber(valueReport?.totalValue ?? 0, locale)}
          icon={Package}
        />
        <StatCard
          label={t("nxtClosing")}
          value={formatNumber(nxtReport?.totalClosingValue ?? 0, locale)}
          icon={BarChart3}
        />
        <StatCard
          label={t("nearExpiry")}
          value={String(nearExpiry?.length ?? 0)}
          icon={Calendar}
        />
        <StatCard
          label={t("expiryCritical")}
          value={String(expiryCritical)}
          icon={Calendar}
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <DataTableCard title={t("nxtTitle")} icon={BarChart3}>
          {nxtLoading ? (
            <p className="p-4 text-slate-500">{tCommon("loading")}</p>
          ) : !nxtReport?.lines.length ? (
            <EmptyTableState message={tCommon("noData")} />
          ) : (
            <div className="table-wrap max-h-96 overflow-auto">
              <table className="data-table text-sm">
                <thead>
                  <tr>
                    <th>SKU</th>
                    <th>{t("opening")}</th>
                    <th>{t("in")}</th>
                    <th>{t("out")}</th>
                    <th>{t("closing")}</th>
                  </tr>
                </thead>
                <tbody>
                  {nxtReport.lines.slice(0, 50).map((line) => (
                    <tr key={`${line.productVariantId}-${line.warehouseId}`}>
                      <td className="font-mono text-xs">{line.sku}</td>
                      <td>{formatNumber(line.openingQuantity, locale)}</td>
                      <td>{formatNumber(line.inQuantity, locale)}</td>
                      <td>{formatNumber(line.outQuantity, locale)}</td>
                      <td>{formatNumber(line.closingQuantity, locale)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </DataTableCard>

        <DataTableCard title={t("nearExpiryTitle")} icon={Calendar}>
          {!nearExpiry?.length ? (
            <EmptyTableState message={t("noNearExpiry")} />
          ) : (
            <div className="table-wrap max-h-96 overflow-auto">
              <table className="data-table text-sm">
                <thead>
                  <tr>
                    <th>{t("lot")}</th>
                    <th>SKU</th>
                    <th>{t("qty")}</th>
                    <th>HSD</th>
                    <th>{t("daysLeft")}</th>
                  </tr>
                </thead>
                <tbody>
                  {nearExpiry.map((lot) => (
                    <tr key={`${lot.stockLotId}-${lot.warehouseId}`}>
                      <td>{lot.lotCode}</td>
                      <td className="font-mono text-xs">{lot.sku}</td>
                      <td>{formatNumber(lot.quantityOnHand, locale)}</td>
                      <td>{lot.expiryDate ? formatDate(lot.expiryDate, locale) : "—"}</td>
                      <td className={lot.daysUntilExpiry <= 7 ? "text-red-600 font-medium" : ""}>
                        {lot.daysUntilExpiry}
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
