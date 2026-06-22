"use client";

import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import {
  fetchInventorySummary,
  fetchLowStock,
  fetchMovementSummary,
  fetchStockByWarehouse,
} from "@/lib/api";
import { formatNumber } from "@/lib/format";
import { useQuery } from "@tanstack/react-query";
import {
  Boxes,
  FileText,
  Package,
  ShoppingCart,
  Tags,
  Truck,
  Warehouse,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

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

  const links = [
    { href: "/products", label: tNav("products"), icon: Package },
    { href: "/product-variants", label: tNav("productVariants"), icon: Tags },
    { href: "/warehouses", label: tNav("warehouses"), icon: Warehouse },
    { href: "/suppliers", label: tNav("suppliers"), icon: Truck },
    { href: "/purchase-orders", label: tNav("purchaseOrders"), icon: ShoppingCart },
    { href: "/inventory-documents", label: tNav("inventoryDocuments"), icon: FileText },
    { href: "/current-stocks", label: tNav("currentStocks"), icon: Boxes },
  ];

  const statCards = summary
    ? [
        { label: t("totalSkus"), value: formatNumber(summary.totalSkus, locale) },
        { label: t("totalOnHand"), value: formatNumber(summary.totalOnHand, locale) },
        { label: t("totalAvailable"), value: formatNumber(summary.totalAvailable, locale) },
        { label: t("openPos"), value: formatNumber(summary.openPurchaseOrders, locale) },
        { label: t("pendingGr"), value: formatNumber(summary.pendingGoodsReceipts, locale) },
      ]
    : [];

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      {summaryLoading ? (
        <p className="mb-6 text-slate-500">{tCommon("loading")}</p>
      ) : (
        <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
          {statCards.map((card) => (
            <div key={card.label} className="card p-4">
              <p className="text-xs text-slate-500">{card.label}</p>
              <p className="mt-1 text-2xl font-bold text-slate-900">{card.value}</p>
            </div>
          ))}
        </div>
      )}

      <div className="mb-8 grid gap-6 lg:grid-cols-2">
        {movements && (
          <div className="card p-5">
            <h2 className="mb-4 font-semibold text-slate-800">{t("movements30d")}</h2>
            <dl className="grid grid-cols-3 gap-4 text-sm">
              <div>
                <dt className="text-slate-500">{t("totalIn")}</dt>
                <dd className="text-lg font-semibold text-green-700">
                  +{formatNumber(movements.totalIn, locale)}
                </dd>
              </div>
              <div>
                <dt className="text-slate-500">{t("totalOut")}</dt>
                <dd className="text-lg font-semibold text-red-700">
                  -{formatNumber(movements.totalOut, locale)}
                </dd>
              </div>
              <div>
                <dt className="text-slate-500">{t("transactions")}</dt>
                <dd className="text-lg font-semibold">
                  {formatNumber(movements.transactionCount, locale)}
                </dd>
              </div>
            </dl>
          </div>
        )}

        <div className="card p-5">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-semibold text-slate-800">{t("lowStock")}</h2>
            <div className="flex items-center gap-2 text-sm">
              <span className="text-slate-500">{t("threshold")}</span>
              <input
                type="number"
                min={0}
                className="input w-20 py-1"
                value={lowStockThreshold}
                onChange={(e) => setLowStockThreshold(Number(e.target.value))}
              />
            </div>
          </div>
          <div className="table-wrap max-h-48 overflow-y-auto">
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
                      <td className="font-mono text-xs">{item.sku}</td>
                      <td>{item.warehouseCode}</td>
                      <td className="text-red-700">
                        {formatNumber(item.quantityAvailable, locale)}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={3} className="text-slate-500">
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
        <div className="card mb-8">
          <div className="border-b border-slate-200 px-4 py-3 font-semibold">
            {t("stockByWarehouse")}
          </div>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("warehouse")}</th>
                  <th>SKUs</th>
                  <th>{tStocks("onHand")}</th>
                  <th>{tStocks("available")}</th>
                </tr>
              </thead>
              <tbody>
                {byWarehouse.map((row) => (
                  <tr key={row.warehouseId}>
                    <td>{row.warehouseCode} — {row.warehouseName}</td>
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

      <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
        {t("quickLinks")}
      </h2>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {links.map(({ href, label, icon: Icon }) => (
          <Link
            key={href}
            href={href}
            className="card flex items-center gap-4 p-5 transition-shadow hover:shadow-md"
          >
            <div className="rounded-lg bg-brand-50 p-3 text-brand-600">
              <Icon className="h-6 w-6" />
            </div>
            <span className="font-medium text-slate-800">{label}</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
