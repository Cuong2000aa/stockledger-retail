"use client";

import { PageHeader } from "@/components/PageHeader";
import { StatCard } from "@/components/StatCard";
import { StatCardsSkeleton } from "@/components/LoadingState";
import { MiniBarChart } from "@/features/insights/components/MiniBarChart";
import { InsightExplainModal } from "@/features/insights/components/InsightExplainModal";
import { InsightFilters } from "@/features/insights/components/InsightFilters";
import {
  EmptyRow,
  InsightSection,
  InsightSkuCell,
  InsightTransferRouteCell,
  InsightWarehouseCell,
  LoadingRow,
} from "@/features/insights/components/InsightSection";
import { InsightsHeroBanner } from "@/features/insights/components/InsightsHeroBanner";
import { InsightTabBar } from "@/features/insights/components/InsightTabBar";
import { RecommendationCard } from "@/features/insights/components/RecommendationCard";
import { getInsightSummaryKey } from "@/features/insights/insight-explain";
import {
  findCtaById,
  getRecommendationDetail,
  getRecommendationTitle,
  resolveRecommendation,
} from "@/features/insights/recommendation-utils";
import type { InsightExplainContext, InsightTab } from "@/features/insights/types";
import { useNotify } from "@/hooks/useNotify";
import { formatNumber } from "@/lib/format";
import type { InsightRecommendation } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/routing";
import clsx from "clsx";
import {
  AlertTriangle,
  ArrowRightLeft,
  PackageX,
  Sparkles,
  TrendingDown,
  TrendingUp,
} from "lucide-react";
import { useCallback, useMemo, useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import {
  createTransferFromCtaPayload,
  createTransferFromSuggestion,
  fetchDeadStockInsights,
  fetchSalesVelocityInsights,
  fetchTransferSuggestions,
} from "@/features/insights/api";
import { insightQueryKeys } from "@/features/insights/queries";
import type {
  DeadStockInsight,
  SalesVelocityInsight,
  TransferSuggestion,
} from "@/lib/types";

const TOP_CHART_COUNT = 8;

type ExplainState = {
  recommendation?: InsightRecommendation;
  severity: string;
  title: string;
  actionDetail: string;
  context: InsightExplainContext;
};

export default function InsightsPage() {
  const t = useTranslations("insights");
  const tActions = useTranslations("insights.actions");
  const tStocks = useTranslations("stocks");
  const tCommon = useTranslations("common");
  const searchParams = useSearchParams();
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { notifyError } = useNotify();

  const [warehouseId, setWarehouseId] = useState("");
  const [daysWithoutOutbound, setDaysWithoutOutbound] = useState(60);
  const [lookbackDays, setLookbackDays] = useState(30);
  const [activeTab, setActiveTab] = useState<InsightTab>("deadStock");
  const [executingActionKey, setExecutingActionKey] = useState<string | null>(null);
  const [explainState, setExplainState] = useState<ExplainState | null>(null);

  useEffect(() => {
    const tab = searchParams.get("tab");
    if (tab === "deadStock" || tab === "velocity" || tab === "transfer") {
      setActiveTab(tab);
    }
  }, [searchParams]);

  const activeWarehouseId = warehouseId || undefined;

  const {
    data: deadStock,
    isLoading: deadStockLoading,
    isFetched: deadStockFetched,
  } = useQuery({
    queryKey: insightQueryKeys.deadStock(activeWarehouseId, daysWithoutOutbound),
    queryFn: () => fetchDeadStockInsights(activeWarehouseId, daysWithoutOutbound, 1, 20),
    enabled: activeTab === "deadStock",
    staleTime: 2 * 60_000,
  });

  const {
    data: salesVelocity,
    isLoading: salesVelocityLoading,
    isFetched: salesVelocityFetched,
  } = useQuery({
    queryKey: insightQueryKeys.salesVelocity(activeWarehouseId, lookbackDays),
    queryFn: () => fetchSalesVelocityInsights(activeWarehouseId, lookbackDays, 20),
    enabled: activeTab === "velocity",
    staleTime: 2 * 60_000,
  });

  const {
    data: transferSuggestions,
    isLoading: transferSuggestionsLoading,
    isFetched: transferSuggestionsFetched,
  } = useQuery({
    queryKey: insightQueryKeys.transferSuggestions(undefined, activeWarehouseId, lookbackDays),
    queryFn: () =>
      fetchTransferSuggestions(undefined, activeWarehouseId, lookbackDays, 14, 7, 20),
    enabled: activeTab === "transfer",
    staleTime: 2 * 60_000,
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

  const stats = useMemo(() => {
    const deadItems = deadStock ?? [];
    const velocityItems = salesVelocity ?? [];
    const transferItems = transferSuggestions ?? [];

    return {
      deadCount: deadStockFetched ? deadItems.length : null,
      tiedCapital: deadStockFetched
        ? deadItems.reduce((sum, item) => sum + (item.estimatedCostValue ?? 0), 0)
        : null,
      urgentVelocity: salesVelocityFetched
        ? velocityItems.filter(
            (item) => item.severity === "critical" || item.severity === "warning"
          ).length
        : null,
      criticalDead: deadStockFetched
        ? deadItems.filter((item) => item.severity === "critical").length
        : null,
      transferCount: transferSuggestionsFetched ? transferItems.length : null,
    };
  }, [
    deadStock,
    salesVelocity,
    transferSuggestions,
    deadStockFetched,
    salesVelocityFetched,
    transferSuggestionsFetched,
  ]);

  const activeTabLoading =
    (activeTab === "deadStock" && deadStockLoading) ||
    (activeTab === "velocity" && salesVelocityLoading) ||
    (activeTab === "transfer" && transferSuggestionsLoading);

  const summaryMeta = getInsightSummaryKey(activeTab, stats);
  let summaryText = "";
  try {
    summaryText = t(summaryMeta.key as never, summaryMeta.values as never);
  } catch {
    summaryText = t("subtitle");
  }

  const deadStockChart = useMemo(() => buildDeadStockChart(deadStock), [deadStock]);
  const velocityChart = useMemo(() => buildVelocityChart(salesVelocity), [salesVelocity]);
  const transferChart = useMemo(
    () => buildTransferChart(transferSuggestions),
    [transferSuggestions]
  );

  const tabs = useMemo(
    () => [
      {
        id: "deadStock" as const,
        label: t("tabs.deadStock"),
        icon: PackageX,
        count: stats.deadCount,
        description: t("tabHints.deadStock"),
      },
      {
        id: "velocity" as const,
        label: t("tabs.velocity"),
        icon: TrendingUp,
        count: stats.urgentVelocity,
        description: t("tabHints.velocity"),
      },
      {
        id: "transfer" as const,
        label: t("tabs.transfer"),
        icon: ArrowRightLeft,
        count: stats.transferCount,
        description: t("tabHints.transfer"),
      },
    ],
    [t, stats.deadCount, stats.urgentVelocity, stats.transferCount]
  );

  const handleTabChange = (tab: InsightTab) => {
    setActiveTab(tab);
    router.replace(`/insights?tab=${tab}`, { scroll: false });
  };

  const handleResetFilters = () => {
    setWarehouseId("");
    setDaysWithoutOutbound(60);
    setLookbackDays(30);
  };

  const openExplain = useCallback(
    (
      recommendation: InsightRecommendation | undefined,
      severity: string,
      context: InsightExplainContext
    ) => {
      if (!recommendation?.actionCode) {
        return;
      }

      setExplainState({
        recommendation,
        severity,
        title: getRecommendationTitle(recommendation, (key) => {
          try {
            return t(key as never);
          } catch {
            return key;
          }
        }),
        actionDetail: getRecommendationDetail(recommendation, (code, params) => {
          try {
            return tActions(code as never, params as never);
          } catch {
            return code;
          }
        }),
        context,
      });
    },
    [t, tActions]
  );

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
          <div className="flex items-center gap-2 rounded-xl bg-gradient-to-r from-violet-500/10 to-indigo-500/10 px-3 py-2 text-xs font-medium text-violet-700 ring-1 ring-violet-200/60">
            <Sparkles className="h-3.5 w-3.5" />
            {t("aiReady")}
          </div>
        }
      />

      <InsightsHeroBanner
        activeTab={activeTab}
        summary={summaryText}
        loading={activeTabLoading}
      />

      {activeTabLoading && !deadStockFetched && !salesVelocityFetched && !transferSuggestionsFetched ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCardWrap active={activeTab === "deadStock"}>
            <StatCard
              label={t("stats.deadSkus")}
              value={
                stats.deadCount == null ? "—" : formatNumber(stats.deadCount, locale)
              }
              icon={PackageX}
              accent="rose"
            />
          </StatCardWrap>
          <StatCardWrap active={activeTab === "deadStock"}>
            <StatCard
              label={t("stats.tiedCapital")}
              value={
                stats.tiedCapital == null ? "—" : formatNumber(stats.tiedCapital, locale)
              }
              icon={TrendingDown}
              accent="amber"
            />
          </StatCardWrap>
          <StatCardWrap active={activeTab === "velocity"}>
            <StatCard
              label={t("stats.lowCover")}
              value={
                stats.urgentVelocity == null
                  ? "—"
                  : formatNumber(stats.urgentVelocity, locale)
              }
              icon={AlertTriangle}
              accent="indigo"
            />
          </StatCardWrap>
          <StatCardWrap active={activeTab === "transfer"}>
            <StatCard
              label={t("stats.transfers")}
              value={
                stats.transferCount == null
                  ? "—"
                  : formatNumber(stats.transferCount, locale)
              }
              icon={ArrowRightLeft}
              accent="sky"
            />
          </StatCardWrap>
        </div>
      )}

      <div className="card overflow-hidden">
        <InsightFilters
          activeTab={activeTab}
          warehouseId={warehouseId}
          onWarehouseChange={setWarehouseId}
          daysWithoutOutbound={daysWithoutOutbound}
          onDaysWithoutOutboundChange={setDaysWithoutOutbound}
          lookbackDays={lookbackDays}
          onLookbackDaysChange={setLookbackDays}
          onReset={handleResetFilters}
        />

        <InsightTabBar tabs={tabs} activeTab={activeTab} onChange={handleTabChange} />

        {activeTab === "deadStock" && (
          <InsightSection
            title={t("deadStock.title")}
            subtitle={t("deadStock.subtitle")}
            chartTitle={t("charts.deadStockValue")}
            accent="rose"
            icon={PackageX}
            loading={deadStockLoading}
            chart={
              <MiniBarChart
                items={deadStockChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
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
                  deadStock.map((item) => {
                    const recommendation = resolveRecommendation(item);
                    return (
                      <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                        <td>
                          <InsightSkuCell sku={item.sku} severity={item.severity} />
                        </td>
                        <td>
                          <InsightWarehouseCell
                            code={item.warehouseCode}
                            name={item.warehouseName}
                          />
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
                            recommendation={recommendation}
                            severity={item.severity}
                            locale={locale}
                            onExplain={() =>
                              openExplain(recommendation, item.severity, {
                                sku: item.sku,
                                warehouseCode: item.warehouseCode,
                                warehouseName: item.warehouseName,
                              })
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

        {activeTab === "velocity" && (
          <InsightSection
            title={t("salesVelocity.title")}
            subtitle={t("salesVelocity.subtitle")}
            chartTitle={t("charts.lowCover")}
            accent="indigo"
            icon={TrendingUp}
            loading={salesVelocityLoading}
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
                  salesVelocity.map((item) => {
                    const recommendation = resolveRecommendation(item);
                    return (
                      <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                        <td>
                          <InsightSkuCell sku={item.sku} severity={item.severity} />
                        </td>
                        <td>
                          <InsightWarehouseCell
                            code={item.warehouseCode}
                            name={item.warehouseName}
                          />
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
                            recommendation={recommendation}
                            severity={item.severity}
                            locale={locale}
                            onExplain={() =>
                              openExplain(recommendation, item.severity, {
                                sku: item.sku,
                                warehouseCode: item.warehouseCode,
                                warehouseName: item.warehouseName,
                              })
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

        {activeTab === "transfer" && (
          <InsightSection
            title={t("transfer.title")}
            subtitle={t("transfer.subtitle")}
            chartTitle={t("charts.transferQty")}
            accent="sky"
            icon={ArrowRightLeft}
            loading={transferSuggestionsLoading}
            chart={
              <MiniBarChart
                items={transferChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
          >
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{t("transfer.route")}</th>
                  <th>{t("transfer.qty")}</th>
                  <th>{t("transfer.coverDays")}</th>
                  <th className="min-w-[20rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {transferSuggestionsLoading ? (
                  <LoadingRow colSpan={5} label={tCommon("loading")} />
                ) : transferSuggestions?.length ? (
                  transferSuggestions.map((item, index) => {
                    const rowKey = `${item.productVariantId}-${item.sourceWarehouseId}-${item.destinationWarehouseId}-${index}`;
                    const recommendation = resolveRecommendation(item);
                    const executingActionId = executingActionKey?.startsWith(`${rowKey}:`)
                      ? executingActionKey.split(":").pop() ?? null
                      : null;

                    return (
                      <tr key={rowKey}>
                        <td>
                          <InsightSkuCell sku={item.sku} severity={item.severity} />
                        </td>
                        <td>
                          <InsightTransferRouteCell
                            from={item.sourceWarehouseCode}
                            to={item.destinationWarehouseCode}
                          />
                        </td>
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
                            recommendation={recommendation}
                            severity={item.severity}
                            locale={locale}
                            executingActionId={executingActionId}
                            onApiAction={(actionId) =>
                              handleTransferApiAction(rowKey, actionId, item)
                            }
                            onExplain={() =>
                              openExplain(recommendation, item.severity, {
                                sku: item.sku,
                                sourceWarehouseCode: item.sourceWarehouseCode,
                                destinationWarehouseCode: item.destinationWarehouseCode,
                              })
                            }
                          />
                        </td>
                      </tr>
                    );
                  })
                ) : (
                  <EmptyRow colSpan={5} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}
      </div>

      <InsightExplainModal
        open={explainState != null}
        onClose={() => setExplainState(null)}
        recommendation={explainState?.recommendation}
        severity={explainState?.severity ?? "info"}
        title={explainState?.title ?? ""}
        actionDetail={explainState?.actionDetail ?? ""}
        context={explainState?.context ?? {}}
      />
    </div>
  );
}

function StatCardWrap({
  active,
  children,
}: {
  active: boolean;
  children: React.ReactNode;
}) {
  return (
    <div
      className={clsx(
        "rounded-2xl transition-all duration-200",
        active && "ring-2 ring-brand-500/40 ring-offset-2 ring-offset-slate-50"
      )}
    >
      {children}
    </div>
  );
}

function buildDeadStockChart(deadStock: DeadStockInsight[] | undefined) {
  if (!Array.isArray(deadStock) || !deadStock.length) {
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
}

function buildVelocityChart(salesVelocity: SalesVelocityInsight[] | undefined) {
  if (!Array.isArray(salesVelocity) || !salesVelocity.length) {
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
}

function buildTransferChart(transferSuggestions: TransferSuggestion[] | undefined) {
  if (!Array.isArray(transferSuggestions) || !transferSuggestions.length) {
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
}
