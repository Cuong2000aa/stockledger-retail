"use client";

import { PageHeader } from "@/components/PageHeader";
import { StatCard } from "@/components/StatCard";
import { StatCardsSkeleton } from "@/components/LoadingState";
import { MiniBarChart } from "@/features/insights/components/MiniBarChart";
import { RecommendationCard } from "@/features/insights/components/RecommendationCard";
import { findCtaById, resolveRecommendation } from "@/features/insights/recommendation-utils";
import { useNotify } from "@/hooks/useNotify";
import { fetchWarehouses } from "@/lib/api";
import { formatNumber } from "@/lib/format";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/routing";
import clsx from "clsx";
import {
  AlertTriangle,
  ArrowRightLeft,
  Calendar,
  Filter,
  PackageX,
  Sparkles,
  TrendingDown,
  TrendingUp,
  Warehouse,
} from "lucide-react";
import { useMemo, useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import {
  createTransferFromCtaPayload,
  createTransferFromSuggestion,
  fetchDeadStockInsights,
  fetchSalesVelocityInsights,
  fetchTransferSuggestions,
} from "@/features/insights/api";
import { insightQueryKeys } from "@/features/insights/queries";
import type { TransferSuggestion } from "@/features/insights/types";

type InsightTab = "deadStock" | "velocity" | "transfer";

const TOP_CHART_COUNT = 8;

export default function InsightsPage() {
  const t = useTranslations("insights");
  const tCommon = useTranslations("common");
  const tStocks = useTranslations("stocks");
  const searchParams = useSearchParams();
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { notifyError } = useNotify();
  const [warehouseId, setWarehouseId] = useState<string>("");
  const [daysWithoutOutbound, setDaysWithoutOutbound] = useState(60);
  const [lookbackDays, setLookbackDays] = useState(30);
  const [activeTab, setActiveTab] = useState<InsightTab>("deadStock");
  const [executingActionKey, setExecutingActionKey] = useState<string | null>(null);

  useEffect(() => {
    const tab = searchParams.get("tab");
    if (tab === "deadStock" || tab === "velocity" || tab === "transfer") {
      setActiveTab(tab);
    }
  }, [searchParams]);

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses", "all-for-insights"],
    queryFn: () => fetchWarehouses(1, 200),
  });

  const activeWarehouseId = warehouseId || undefined;

  const { data: deadStock, isLoading: deadStockLoading } = useQuery({
    queryKey: insightQueryKeys.deadStock(activeWarehouseId, daysWithoutOutbound),
    queryFn: () => fetchDeadStockInsights(activeWarehouseId, daysWithoutOutbound, 1, 20),
  });

  const { data: salesVelocity, isLoading: salesVelocityLoading } = useQuery({
    queryKey: insightQueryKeys.salesVelocity(activeWarehouseId, lookbackDays),
    queryFn: () => fetchSalesVelocityInsights(activeWarehouseId, lookbackDays, 20),
  });

  const { data: transferSuggestions, isLoading: transferSuggestionsLoading } = useQuery({
    queryKey: insightQueryKeys.transferSuggestions(undefined, activeWarehouseId, lookbackDays),
    queryFn: () => fetchTransferSuggestions(undefined, activeWarehouseId, lookbackDays, 14, 7, 20),
  });

  const createTransferMutation = useMutation({
    mutationFn: createTransferFromSuggestion,
    onSuccess: (doc) => {
      setExecutingActionKey(null);
      void queryClient.invalidateQueries({ queryKey: ["inventory-documents"] });
      router.push(`/inventory-documents/${doc.id}`);
    },
    onError: (error) => {
      setExecutingActionKey(null);
      notifyError(error);
    },
  });

  const createTransferFromCtaMutation = useMutation({
    mutationFn: createTransferFromCtaPayload,
    onSuccess: (doc) => {
      setExecutingActionKey(null);
      void queryClient.invalidateQueries({ queryKey: ["inventory-documents"] });
      router.push(`/inventory-documents/${doc.id}`);
    },
    onError: (error) => {
      setExecutingActionKey(null);
      notifyError(error);
    },
  });

  const warehouseOptions = useMemo(
    () => warehouses?.items ?? [],
    [warehouses?.items]
  );

  const stats = useMemo(() => {
    const deadItems = deadStock ?? [];
    const velocityItems = salesVelocity ?? [];
    const transferItems = transferSuggestions ?? [];

    const tiedCapital = deadItems.reduce(
      (sum, item) => sum + (item.estimatedCostValue ?? 0),
      0
    );
    const urgentVelocity = velocityItems.filter(
      (item) => item.severity === "critical" || item.severity === "warning"
    ).length;
    const criticalDead = deadItems.filter((item) => item.severity === "critical").length;

    return {
      deadCount: deadItems.length,
      tiedCapital,
      urgentVelocity,
      criticalDead,
      transferCount: transferItems.length,
    };
  }, [deadStock, salesVelocity, transferSuggestions]);

  const deadStockChart = useMemo(() => {
    if (!deadStock?.length) {
      return [];
    }

    return [...deadStock]
      .sort((a, b) => (b.estimatedCostValue ?? 0) - (a.estimatedCostValue ?? 0))
      .slice(0, TOP_CHART_COUNT)
      .map((item) => ({
        id: `${item.productVariantId}-${item.warehouseId}`,
        label: item.sku,
        sublabel: item.warehouseCode,
        value: item.estimatedCostValue ?? 0,
        severity: item.severity,
      }));
  }, [deadStock]);

  const velocityChart = useMemo(() => {
    if (!salesVelocity?.length) {
      return [];
    }

    return [...salesVelocity]
      .filter((item) => item.estimatedDaysOfCover != null)
      .sort((a, b) => (a.estimatedDaysOfCover ?? 999) - (b.estimatedDaysOfCover ?? 999))
      .slice(0, TOP_CHART_COUNT)
      .map((item) => ({
        id: `${item.productVariantId}-${item.warehouseId}`,
        label: item.sku,
        sublabel: item.warehouseCode,
        value: item.estimatedDaysOfCover ?? 0,
        severity: item.severity,
      }));
  }, [salesVelocity]);

  const transferChart = useMemo(() => {
    if (!transferSuggestions?.length) {
      return [];
    }

    return [...transferSuggestions]
      .sort((a, b) => b.suggestedQuantity - a.suggestedQuantity)
      .slice(0, TOP_CHART_COUNT)
      .map((item, index) => ({
        id: `${item.productVariantId}-${item.sourceWarehouseId}-${index}`,
        label: item.sku,
        sublabel: `${item.sourceWarehouseCode} → ${item.destinationWarehouseCode}`,
        value: item.suggestedQuantity,
        severity: item.severity,
      }));
  }, [transferSuggestions]);

  const isLoading =
    deadStockLoading || salesVelocityLoading || transferSuggestionsLoading;

  const tabs: { id: InsightTab; label: string; icon: typeof PackageX; count: number }[] = [
    { id: "deadStock", label: t("tabs.deadStock"), icon: PackageX, count: stats.deadCount },
    { id: "velocity", label: t("tabs.velocity"), icon: TrendingUp, count: stats.urgentVelocity },
    { id: "transfer", label: t("tabs.transfer"), icon: ArrowRightLeft, count: stats.transferCount },
  ];

  const handleTransferApiAction = (
    rowKey: string,
    actionId: string,
    item: TransferSuggestion
  ) => {
    const actionKey = `${rowKey}:${actionId}`;
    const cta = findCtaById(resolveRecommendation(item)?.actions, actionId);
    setExecutingActionKey(actionKey);

    if (cta?.payload && Object.keys(cta.payload).length > 0) {
      createTransferFromCtaMutation.mutate(cta.payload);
      return;
    }

    createTransferMutation.mutate(item);
  };

  return (
    <div>
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <div className="flex items-center gap-2 rounded-xl bg-gradient-to-r from-rose-500/10 to-violet-500/10 px-3 py-2 text-xs font-medium text-violet-700 ring-1 ring-violet-200/60">
            <Sparkles className="h-3.5 w-3.5" />
            {t("aiReady")}
          </div>
        }
      />

      {isLoading ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard
            label={t("stats.deadSkus")}
            value={formatNumber(stats.deadCount, locale)}
            icon={PackageX}
            accent="rose"
          />
          <StatCard
            label={t("stats.tiedCapital")}
            value={formatNumber(stats.tiedCapital, locale)}
            icon={TrendingDown}
            accent="amber"
          />
          <StatCard
            label={t("stats.lowCover")}
            value={formatNumber(stats.urgentVelocity, locale)}
            icon={AlertTriangle}
            accent="indigo"
          />
          <StatCard
            label={t("stats.transfers")}
            value={formatNumber(stats.transferCount, locale)}
            icon={ArrowRightLeft}
            accent="sky"
          />
        </div>
      )}

      <div className="card mb-6 overflow-hidden">
        <div className="border-b border-slate-100 bg-gradient-to-r from-slate-50 to-white px-5 py-4">
          <div className="mb-4 flex items-center gap-2 text-sm font-semibold text-slate-700">
            <Filter className="h-4 w-4 text-brand-500" />
            {t("filters.title")}
          </div>
          <div className="grid gap-4 md:grid-cols-3">
            <label className="text-sm text-slate-600">
              <span className="mb-1.5 flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-slate-500">
                <Warehouse className="h-3.5 w-3.5" />
                {t("filters.warehouse")}
              </span>
              <select
                className="input"
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
              >
                <option value="">{t("filters.allWarehouses")}</option>
                {warehouseOptions.map((warehouse) => (
                  <option key={warehouse.id} value={warehouse.id}>
                    {warehouse.code} - {warehouse.name}
                  </option>
                ))}
              </select>
            </label>

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
                onChange={(e) => setDaysWithoutOutbound(Number(e.target.value) || 1)}
              />
            </label>

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
                onChange={(e) => setLookbackDays(Number(e.target.value) || 1)}
              />
            </label>
          </div>
        </div>

        <div className="flex flex-wrap gap-2 border-b border-slate-100 px-4 py-3">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;

            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={clsx(
                  "inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-medium transition-all",
                  isActive
                    ? "bg-slate-900 text-white shadow-md"
                    : "bg-slate-50 text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                )}
              >
                <Icon className="h-4 w-4" />
                {tab.label}
                <span
                  className={clsx(
                    "rounded-full px-2 py-0.5 text-xs font-semibold tabular-nums",
                    isActive ? "bg-white/20 text-white" : "bg-white text-slate-700 ring-1 ring-slate-200"
                  )}
                >
                  {tab.count}
                </span>
              </button>
            );
          })}
        </div>

        {activeTab === "deadStock" && (
          <InsightSection
            title={t("deadStock.title")}
            subtitle={t("deadStock.subtitle")}
            chartTitle={t("charts.deadStockValue")}
            chart={
              <MiniBarChart
                items={deadStockChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
            loading={deadStockLoading}
          >
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("deadStock.days")}</th>
                  <th>{tStocks("onHand")}</th>
                  <th>{t("deadStock.costValue")}</th>
                  <th className="min-w-[20rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {deadStockLoading ? (
                  <LoadingRow colSpan={6} label={tCommon("loading")} />
                ) : deadStock?.length ? (
                  deadStock.map((item) => (
                    <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                      <td>
                        <span className="rounded-lg bg-slate-100 px-2 py-1 font-mono text-xs font-semibold text-slate-800">
                          {item.sku}
                        </span>
                      </td>
                      <td>
                        <p className="font-medium text-slate-800">{item.warehouseCode}</p>
                        <p className="text-xs text-slate-500">{item.warehouseName}</p>
                      </td>
                      <td className="tabular-nums font-semibold">
                        {formatNumber(item.daysWithoutOutbound, locale)}
                      </td>
                      <td className="tabular-nums">
                        {formatNumber(item.quantityOnHand, locale)}
                      </td>
                      <td className="tabular-nums font-medium text-amber-700">
                        {formatNumber(item.estimatedCostValue ?? 0, locale)}
                      </td>
                      <td>
                        <RecommendationCard
                          recommendation={resolveRecommendation(item)}
                          severity={item.severity}
                          locale={locale}
                        />
                      </td>
                    </tr>
                  ))
                ) : (
                  <EmptyRow colSpan={6} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}

        {activeTab === "velocity" && (
          <InsightSection
            title={t("salesVelocity.title")}
            subtitle={t("salesVelocity.subtitle")}
            chartTitle={t("charts.lowCover")}
            chart={
              <MiniBarChart
                items={velocityChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) =>
                  `${formatNumber(value, locale)} ${t("salesVelocity.coverDays").toLowerCase()}`
                }
              />
            }
            loading={salesVelocityLoading}
          >
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("salesVelocity.outbound")}</th>
                  <th>{t("salesVelocity.avgDaily")}</th>
                  <th>{t("salesVelocity.coverDays")}</th>
                  <th className="min-w-[20rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {salesVelocityLoading ? (
                  <LoadingRow colSpan={6} label={tCommon("loading")} />
                ) : salesVelocity?.length ? (
                  salesVelocity.map((item) => (
                    <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                      <td>
                        <span className="rounded-lg bg-slate-100 px-2 py-1 font-mono text-xs font-semibold text-slate-800">
                          {item.sku}
                        </span>
                      </td>
                      <td>
                        <p className="font-medium text-slate-800">{item.warehouseCode}</p>
                        <p className="text-xs text-slate-500">{item.warehouseName}</p>
                      </td>
                      <td className="tabular-nums">
                        {formatNumber(item.outboundQuantity, locale)}
                      </td>
                      <td className="tabular-nums">
                        {formatNumber(item.averageDailyOutbound, locale)}
                      </td>
                      <td className="tabular-nums font-semibold">
                        {item.estimatedDaysOfCover != null
                          ? formatNumber(item.estimatedDaysOfCover, locale)
                          : t("salesVelocity.noDemand")}
                      </td>
                      <td>
                        <RecommendationCard
                          recommendation={resolveRecommendation(item)}
                          severity={item.severity}
                          locale={locale}
                        />
                      </td>
                    </tr>
                  ))
                ) : (
                  <EmptyRow colSpan={6} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}

        {activeTab === "transfer" && (
          <InsightSection
            title={t("transfer.title")}
            subtitle={t("transfer.subtitle")}
            chartTitle={t("charts.transferQty")}
            chart={
              <MiniBarChart
                items={transferChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
            loading={transferSuggestionsLoading}
          >
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{t("transfer.from")}</th>
                  <th>{t("transfer.to")}</th>
                  <th>{t("transfer.qty")}</th>
                  <th>{t("transfer.coverDays")}</th>
                  <th className="min-w-[20rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {transferSuggestionsLoading ? (
                  <LoadingRow colSpan={6} label={tCommon("loading")} />
                ) : transferSuggestions?.length ? (
                  transferSuggestions.map((item, index) => {
                    const rowKey = `${item.productVariantId}-${item.sourceWarehouseId}-${item.destinationWarehouseId}-${index}`;
                    const executingActionId = executingActionKey?.startsWith(`${rowKey}:`)
                      ? executingActionKey.split(":").pop() ?? null
                      : null;

                    return (
                      <tr key={rowKey}>
                        <td>
                          <span className="rounded-lg bg-slate-100 px-2 py-1 font-mono text-xs font-semibold text-slate-800">
                            {item.sku}
                          </span>
                        </td>
                        <td className="font-medium">{item.sourceWarehouseCode}</td>
                        <td className="font-medium">{item.destinationWarehouseCode}</td>
                        <td className="tabular-nums font-semibold text-sky-700">
                          {formatNumber(item.suggestedQuantity, locale)}
                        </td>
                        <td className="tabular-nums">
                          {item.destinationDaysOfCover != null
                            ? formatNumber(item.destinationDaysOfCover, locale)
                            : tCommon("noData")}
                        </td>
                        <td>
                          <RecommendationCard
                            recommendation={resolveRecommendation(item)}
                            severity={item.severity}
                            locale={locale}
                            executingActionId={executingActionId}
                            onApiAction={(actionId) =>
                              handleTransferApiAction(rowKey, actionId, item)
                            }
                          />
                        </td>
                      </tr>
                    );
                  })
                ) : (
                  <EmptyRow colSpan={6} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}
      </div>
    </div>
  );
}

function InsightSection({
  title,
  subtitle,
  chartTitle,
  chart,
  loading,
  children,
}: {
  title: string;
  subtitle: string;
  chartTitle: string;
  chart: React.ReactNode;
  loading: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="grid gap-0 xl:grid-cols-[minmax(16rem,22rem)_1fr]">
      <div className="border-b border-slate-100 p-5 xl:border-b-0 xl:border-r">
        <h3 className="text-sm font-semibold text-slate-900">{chartTitle}</h3>
        <p className="mt-1 text-xs text-slate-500">{subtitle}</p>
        <div className="mt-4">
          {loading ? (
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, index) => (
                <div key={index} className="skeleton h-10 w-full" />
              ))}
            </div>
          ) : (
            chart
          )}
        </div>
      </div>

      <div>
        <div className="border-b border-slate-100 px-5 py-4">
          <h2 className="font-semibold text-slate-900">{title}</h2>
          <p className="mt-1 text-sm text-slate-500">{subtitle}</p>
        </div>
        <div className="table-wrap max-h-[32rem] overflow-y-auto scrollbar-thin">
          {children}
        </div>
      </div>
    </div>
  );
}

function LoadingRow({ colSpan, label }: { colSpan: number; label: string }) {
  return (
    <tr>
      <td colSpan={colSpan} className="py-8 text-center text-slate-500">
        {label}
      </td>
    </tr>
  );
}

function EmptyRow({ colSpan, label }: { colSpan: number; label: string }) {
  return (
    <tr>
      <td colSpan={colSpan} className="py-12 text-center text-slate-500">
        {label}
      </td>
    </tr>
  );
}
