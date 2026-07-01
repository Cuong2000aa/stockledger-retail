"use client";

import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { StatCard } from "@/components/StatCard";
import { StatCardsSkeleton } from "@/components/LoadingState";
import {
  fetchInventorySummary,
  fetchLowStock,
  fetchMovementSummary,
  fetchStockByWarehouse,
} from "@/lib/api";
import { formatNumber } from "@/lib/format";
import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  ArrowDownLeft,
  ArrowUpRight,
  Boxes,
  ChevronRight,
  FileText,
  Layers,
  Lightbulb,
  Package,
  ShoppingCart,
  Tags,
  Truck,
  Warehouse,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";

const WAREHOUSE_PREVIEW_LIMIT = 10;

export default function DashboardPage() {
  const t = useTranslations("dashboard");
  const tNav = useTranslations("nav");
  const tStocks = useTranslations("stocks");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const [lowStockThreshold, setLowStockThreshold] = useState(10);

  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ["inventory-summary"],
    queryFn: fetchInventorySummary,
  });

  const { data: byWarehouse } = useQuery({
    queryKey: ["stock-by-warehouse"],
    queryFn: fetchStockByWarehouse,
  });

  const { data: movements } = useQuery({
    queryKey: ["movement-summary"],
    queryFn: () => fetchMovementSummary(),
  });

  const { data: lowStock } = useQuery({
    queryKey: ["low-stock", lowStockThreshold],
    queryFn: () => fetchLowStock(lowStockThreshold),
  });

  const warehousePreview = useMemo(() => {
    if (!byWarehouse?.length) {
      return [];
    }

    return [...byWarehouse]
      .sort((a, b) => b.totalOnHand - a.totalOnHand)
      .slice(0, WAREHOUSE_PREVIEW_LIMIT);
  }, [byWarehouse]);

  const links = [
    { href: "/products", label: tNav("products"), icon: Package, accent: "from-indigo-500/15 to-indigo-600/5 text-indigo-600" },
    { href: "/product-variants", label: tNav("productVariants"), icon: Tags, accent: "from-violet-500/15 to-violet-600/5 text-violet-600" },
    { href: "/warehouses", label: tNav("warehouses"), icon: Warehouse, accent: "from-sky-500/15 to-sky-600/5 text-sky-600" },
    { href: "/suppliers", label: tNav("suppliers"), icon: Truck, accent: "from-emerald-500/15 to-emerald-600/5 text-emerald-600" },
    { href: "/purchase-orders", label: tNav("purchaseOrders"), icon: ShoppingCart, accent: "from-amber-500/15 to-amber-600/5 text-amber-600" },
    { href: "/insights", label: tNav("insights"), icon: Lightbulb, accent: "from-rose-500/15 to-rose-600/5 text-rose-600" },
    { href: "/inventory-documents", label: tNav("inventoryDocuments"), icon: FileText, accent: "from-brand-500/15 to-brand-600/5 text-brand-600" },
    { href: "/current-stocks", label: tNav("currentStocks"), icon: Boxes, accent: "from-teal-500/15 to-teal-600/5 text-teal-600" },
  ];

  const statConfig = summary
    ? [
        { label: t("totalSkus"), value: formatNumber(summary.totalSkus, locale), icon: Layers, accent: "indigo" as const },
        { label: t("totalOnHand"), value: formatNumber(summary.totalOnHand, locale), icon: Boxes, accent: "emerald" as const },
        { label: t("totalAvailable"), value: formatNumber(summary.totalAvailable, locale), icon: Package, accent: "sky" as const },
        { label: t("openPos"), value: formatNumber(summary.openPurchaseOrders, locale), icon: ShoppingCart, accent: "amber" as const },
        { label: t("pendingGr"), value: formatNumber(summary.pendingGoodsReceipts, locale), icon: FileText, accent: "rose" as const },
      ]
    : [];

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      {summaryLoading ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
          {statConfig.map((card) => (
            <StatCard key={card.label} {...card} />
          ))}
        </div>
      )}

      <div className="mb-8 grid gap-6 lg:grid-cols-2">
        {movements && (
          <div className="card overflow-hidden">
            <div className="card-header">
              <h2 className="font-semibold text-slate-900">{t("movements30d")}</h2>
            </div>
            <div className="grid grid-cols-3 gap-4 p-5">
              <div className="rounded-xl bg-emerald-50 p-4 ring-1 ring-emerald-100">
                <div className="mb-2 flex items-center gap-2 text-emerald-600">
                  <ArrowDownLeft className="h-4 w-4" />
                  <span className="text-xs font-semibold uppercase tracking-wide">
                    {t("totalIn")}
                  </span>
                </div>
                <p className="text-2xl font-bold text-emerald-700">
                  +{formatNumber(movements.totalIn, locale)}
                </p>
              </div>
              <div className="rounded-xl bg-red-50 p-4 ring-1 ring-red-100">
                <div className="mb-2 flex items-center gap-2 text-red-600">
                  <ArrowUpRight className="h-4 w-4" />
                  <span className="text-xs font-semibold uppercase tracking-wide">
                    {t("totalOut")}
                  </span>
                </div>
                <p className="text-2xl font-bold text-red-700">
                  -{formatNumber(movements.totalOut, locale)}
                </p>
              </div>
              <div className="rounded-xl bg-slate-50 p-4 ring-1 ring-slate-100">
                <div className="mb-2 flex items-center gap-2 text-slate-600">
                  <Layers className="h-4 w-4" />
                  <span className="text-xs font-semibold uppercase tracking-wide">
                    {t("transactions")}
                  </span>
                </div>
                <p className="text-2xl font-bold text-slate-900">
                  {formatNumber(movements.transactionCount, locale)}
                </p>
              </div>
            </div>
            {(movements.transferIn > 0 || movements.transferOut > 0) && (
              <p className="border-t border-slate-100 px-5 py-3 text-xs text-slate-500">
                {t("transferMovementsNote", {
                  in: formatNumber(movements.transferIn, locale),
                  out: formatNumber(movements.transferOut, locale),
                })}
              </p>
            )}
          </div>
        )}

        <div className="card overflow-hidden">
          <div className="card-header flex items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 text-amber-500" />
              <h2 className="font-semibold text-slate-900">{t("lowStock")}</h2>
            </div>
            <div className="flex items-center gap-2 text-sm">
              <span className="text-slate-500">{t("threshold")}</span>
              <input
                type="number"
                min={0}
                className="input w-20 py-1.5 text-center"
                value={lowStockThreshold}
                onChange={(e) => setLowStockThreshold(Number(e.target.value))}
              />
            </div>
          </div>
          <div className="table-wrap max-h-52 overflow-y-auto scrollbar-thin">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{tStocks("available")}</th>
                </tr>
              </thead>
              <tbody>
                {lowStock?.length ? (
                  lowStock.map((item) => (
                    <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                      <td className="font-mono text-xs font-medium">{item.sku}</td>
                      <td>{item.warehouseCode}</td>
                      <td>
                        <span className="badge bg-red-50 text-red-700 ring-red-200">
                          {formatNumber(item.quantityAvailable, locale)}
                        </span>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={3} className="py-8 text-center text-slate-400">
                      {tCommon("noData")}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {byWarehouse && byWarehouse.length > 0 && (
        <div className="card mb-8 overflow-hidden">
          <div className="card-header flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="font-semibold text-slate-900">{t("stockByWarehouse")}</h2>
              {byWarehouse.length > WAREHOUSE_PREVIEW_LIMIT && (
                <p className="mt-1 text-xs text-slate-500">
                  {t("warehousePreviewHint", {
                    shown: WAREHOUSE_PREVIEW_LIMIT,
                    total: byWarehouse.length,
                  })}
                </p>
              )}
            </div>
            <Link
              href="/current-stocks"
              className="text-sm font-semibold text-brand-600 transition hover:text-brand-700"
            >
              {t("viewAllStock")} →
            </Link>
          </div>
          <div className="table-wrap max-h-72 overflow-y-auto scrollbar-thin">
            <table className="data-table">
              <thead className="sticky top-0 z-10 bg-slate-50/95 backdrop-blur-sm">
                <tr>
                  <th>{tStocks("warehouse")}</th>
                  <th>SKUs</th>
                  <th>{tStocks("onHand")}</th>
                  <th>{tStocks("available")}</th>
                </tr>
              </thead>
              <tbody>
                {warehousePreview.map((row) => (
                  <tr key={row.warehouseId}>
                    <td className="font-medium">
                      {row.warehouseCode}
                      <span className="ml-1 text-slate-400">— {row.warehouseName}</span>
                    </td>
                    <td>{formatNumber(row.skuCount, locale)}</td>
                    <td>{formatNumber(row.totalOnHand, locale)}</td>
                    <td>{formatNumber(row.totalAvailable, locale)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <h2 className="mb-4 text-xs font-bold uppercase tracking-widest text-slate-400">
        {t("quickLinks")}
      </h2>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {links.map(({ href, label, icon: Icon, accent }) => (
          <Link
            key={href}
            href={href}
            className="card-interactive group flex items-center gap-4 p-4"
          >
            <div
              className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br ${accent}`}
            >
              <Icon className="h-5 w-5" />
            </div>
            <span className="flex-1 font-medium text-slate-800 group-hover:text-brand-700">
              {label}
            </span>
            <ChevronRight className="h-4 w-4 text-slate-300 transition group-hover:translate-x-0.5 group-hover:text-brand-500" />
          </Link>
        ))}
      </div>
    </div>
  );
}
