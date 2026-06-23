"use client";

import { PageHeader } from "@/components/PageHeader";
import { StatCard } from "@/components/StatCard";
import { StatCardsSkeleton } from "@/components/LoadingState";
import { MiniBarChart } from "@/features/insights/components/MiniBarChart";
import { SeverityBadge } from "@/features/insights/components/SeverityBadge";
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
import { useMemo, useState } from "react";
import {
  createTransferFromSuggestion,
  fetchDeadStockInsights,
  fetchSalesVelocityInsights,
  fetchTransferSuggestions,
} from "@/features/insights/api";
import { insightQueryKeys } from "@/features/insights/queries";
import type { TransferSuggestion } from "@/features/insights/types";

type RecommendationParams = Record<string, string>;
type InsightTab = "deadStock" | "velocity" | "transfer";

const TOP_CHART_COUNT = 8;

export default function InsightsPage() {
  const t = useTranslations("insights");
  const tActions = useTranslations("insights.actions");
  const tCommon = useTranslations("common");
  const tStocks = useTranslations("stocks");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { notifyError } = useNotify();
  const [warehouseId, setWarehouseId] = useState<string>("");
  const [daysWithoutOutbound, setDaysWithoutOutbound] = useState(60);
  const [lookbackDays, setLookbackDays] = useState(30);
  const [activeTab, setActiveTab] = useState<InsightTab>("deadStock");
  const [creatingTransferKey, setCreatingTransferKey] = useState<string | null>(null);

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
      setCreatingTransferKey(null);
      void queryClient.invalidateQueries({ queryKey: ["inventory-documents"] });
      router.push(`/inventory-documents/${doc.id}`);
    },
    onError: (error) => {
      setCreatingTransferKey(null);
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

  const handleCreateTransfer = (item: TransferSuggestion, rowKey: string) => {
    setCreatingTransferKey(rowKey);
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
                  <th>{t("severity.label")}</th>
                  <th>{t("recommendation")}</th>
                </tr>
              </thead>
              <tbody>
                {deadStockLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
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
                        <SeverityBadge
                          severity={item.severity}
                          label={severityLabel(t, item.severity)}
                        />
                      </td>
                      <td className="max-w-xs">
                        <RecommendationBox
                          actionCode={item.recommendedActionCode}
                          params={item.recommendationParams}
                          severity={item.severity}
                          translate={tActions}
                        />
                      </td>
                    </tr>
                  ))
                ) : (
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
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
                  <th>{t("severity.label")}</th>
                  <th>{t("recommendation")}</th>
                </tr>
              </thead>
              <tbody>
                {salesVelocityLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
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
                        <SeverityBadge
                          severity={item.severity}
                          label={severityLabel(t, item.severity)}
                        />
                      </td>
                      <td className="max-w-xs">
                        <RecommendationBox
                          actionCode={item.recommendedActionCode}
                          params={item.recommendationParams}
                          severity={item.severity}
                          translate={tActions}
                        />
                      </td>
                    </tr>
                  ))
                ) : (
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
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
                  <th>{t("severity.label")}</th>
                  <th>{t("recommendation")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {transferSuggestionsLoading ? (
                  <LoadingRow colSpan={8} label={tCommon("loading")} />
                ) : transferSuggestions?.length ? (
                  transferSuggestions.map((item, index) => {
                    const rowKey = `${item.productVariantId}-${item.sourceWarehouseId}-${item.destinationWarehouseId}-${index}`;
                    const isCreating = creatingTransferKey === rowKey;

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
                          <SeverityBadge
                            severity={item.severity}
                            label={severityLabel(t, item.severity)}
                          />
                        </td>
                        <td className="max-w-xs">
                          <RecommendationBox
                            actionCode={item.recommendedActionCode}
                            params={item.recommendationParams}
                            severity={item.severity}
                            translate={tActions}
                          />
                        </td>
                        <td className="whitespace-nowrap">
                          <button
                            type="button"
                            className="btn-primary text-xs"
                            disabled={isCreating || createTransferMutation.isPending}
                            onClick={() => handleCreateTransfer(item, rowKey)}
                          >
                            {isCreating ? t("transfer.creating") : t("transfer.createTransfer")}
                          </button>
                        </td>
                      </tr>
                    );
                  })
                ) : (
                  <EmptyRow colSpan={8} label={tCommon("noData")} />
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

function RecommendationBox({
  actionCode,
  params,
  severity,
  translate,
}: {
  actionCode?: string;
  params?: RecommendationParams;
  severity: string;
  translate: ReturnType<typeof useTranslations<"insights.actions">>;
}) {
  if (!actionCode) {
    return <span className="text-slate-400">—</span>;
  }

  let text = actionCode;
  try {
    text = translate(actionCode as never, params ?? {});
  } catch {
    // keep action code
  }

  const borderColor =
    severity === "critical"
      ? "border-red-300 bg-red-50/50"
      : severity === "warning"
        ? "border-amber-300 bg-amber-50/50"
        : "border-sky-200 bg-slate-50/80";

  return (
    <p
      className={clsx(
        "line-clamp-2 rounded-lg border-l-[3px] px-2.5 py-1.5 text-xs leading-relaxed text-slate-600",
        borderColor
      )}
      title={text}
    >
      {text}
    </p>
  );
}

function severityLabel(
  t: ReturnType<typeof useTranslations<"insights">>,
  severity: string
) {
  if (severity === "critical") {
    return t("severity.critical");
  }
  if (severity === "warning") {
    return t("severity.warning");
  }
  return t("severity.info");
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
