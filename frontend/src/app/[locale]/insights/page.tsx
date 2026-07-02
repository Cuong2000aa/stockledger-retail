"use client";

import { PageHeader } from "@/components/PageHeader";
import { MiniBarChart } from "@/features/insights/components/MiniBarChart";
import { InsightExplainModal } from "@/features/insights/components/InsightExplainModal";
import { InsightFilters } from "@/features/insights/components/InsightFilters";
import {
  EmptyRow,
  InsightSection,
  InsightSkuCell,
  InsightTransferRouteCell,
  InsightWarehouseCell,
  InsightPriorityCell,
  LoadingRow,
} from "@/features/insights/components/InsightSection";
import { InsightsExecutiveSummaryStrip } from "@/features/insights/components/InsightsExecutiveSummaryStrip";
import { InsightsHeroBanner } from "@/features/insights/components/InsightsHeroBanner";
import { InsightTabBar } from "@/features/insights/components/InsightTabBar";
import { InsightTabContextBanner } from "@/features/insights/components/InsightTabContextBanner";
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
import {
  ArrowRightLeft,
  BadgeDollarSign,
  Layers,
  PackageX,
  Percent,
  ShoppingCart,
  Snowflake,
  TrendingUpDown,
  TrendingUp,
} from "lucide-react";
import { useCallback, useMemo, useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import { useWarehouseScope } from "@/hooks/useWarehouseScope";
import {
  createTransferFromCtaPayload,
  createTransferFromSuggestion,
  createBulkTransfersFromInsights,
  fetchBrokenSizeRuns,
  fetchDeadStockInsights,
  fetchInsightsExecutiveSummary,
  fetchMarkdownCandidates,
  fetchPromotionRiskInsights,
  fetchReorderRiskInsights,
  fetchSalesVelocityInsights,
  fetchSeasonClearanceInsights,
  fetchTrendSummaryInsights,
  fetchTransferSuggestions,
  recordInsightAction,
} from "@/features/insights/api";
import { insightQueryKeys } from "@/features/insights/queries";
import type {
  BrokenSizeRunInsight,
  DeadStockInsight,
  MarkdownCandidateInsight,
  PromotionRiskInsight,
  ReorderRiskInsight,
  SalesVelocityInsight,
  SeasonClearanceInsight,
  TrendSummaryInsight,
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
  const { defaultWarehouseId, canSelectAllWarehouses } = useWarehouseScope();

  const [warehouseId, setWarehouseId] = useState("");
  const [brandId, setBrandId] = useState("");
  const [daysWithoutOutbound, setDaysWithoutOutbound] = useState(60);
  const [lookbackDays, setLookbackDays] = useState(30);
  const [activeTab, setActiveTab] = useState<InsightTab>("deadStock");
  const [executingActionKey, setExecutingActionKey] = useState<string | null>(null);
  const [selectedTransferKeys, setSelectedTransferKeys] = useState<string[]>([]);
  const [explainState, setExplainState] = useState<ExplainState | null>(null);

  useEffect(() => {
    const tab = searchParams.get("tab");
    if (
      tab === "deadStock" ||
      tab === "velocity" ||
      tab === "transfer" ||
      tab === "markdown" ||
      tab === "promotionRisk" ||
      tab === "reorderRisk" ||
      tab === "trend" ||
      tab === "brokenSize" ||
      tab === "seasonClearance"
    ) {
      setActiveTab(tab);
    }
  }, [searchParams]);

  const insightScope = {
    warehouseId: warehouseId || undefined,
    brandId: brandId || undefined,
  };

  const {
    data: executiveSummary,
    isLoading: executiveSummaryLoading,
  } = useQuery({
    queryKey: insightQueryKeys.executiveSummary(insightScope, lookbackDays, daysWithoutOutbound),
    queryFn: () => fetchInsightsExecutiveSummary(insightScope, lookbackDays, daysWithoutOutbound),
    staleTime: 2 * 60_000,
  });

  const {
    data: deadStock,
    isLoading: deadStockLoading,
    isFetched: deadStockFetched,
  } = useQuery({
    queryKey: insightQueryKeys.deadStock(insightScope, daysWithoutOutbound),
    queryFn: () => fetchDeadStockInsights(insightScope, daysWithoutOutbound, 1, 20),
    enabled: activeTab === "deadStock",
    staleTime: 2 * 60_000,
  });

  const {
    data: salesVelocity,
    isLoading: salesVelocityLoading,
    isFetched: salesVelocityFetched,
  } = useQuery({
    queryKey: insightQueryKeys.salesVelocity(insightScope, lookbackDays),
    queryFn: () => fetchSalesVelocityInsights(insightScope, lookbackDays, 20),
    enabled: activeTab === "velocity",
    staleTime: 2 * 60_000,
  });

  const {
    data: transferSuggestions,
    isLoading: transferSuggestionsLoading,
    isFetched: transferSuggestionsFetched,
  } = useQuery({
    queryKey: insightQueryKeys.transferSuggestions(
      undefined,
      insightScope.warehouseId,
      lookbackDays,
      insightScope.brandId
    ),
    queryFn: () =>
      fetchTransferSuggestions(
        undefined,
        insightScope.warehouseId,
        lookbackDays,
        14,
        7,
        20,
        insightScope.brandId
      ),
    enabled: activeTab === "transfer",
    staleTime: 2 * 60_000,
  });

  const {
    data: markdownCandidates,
    isLoading: markdownCandidatesLoading,
    isFetched: markdownCandidatesFetched,
  } = useQuery({
    queryKey: insightQueryKeys.markdownCandidates(insightScope, daysWithoutOutbound),
    queryFn: () => fetchMarkdownCandidates(insightScope, daysWithoutOutbound, 1, 20),
    enabled: activeTab === "markdown",
    staleTime: 2 * 60_000,
  });

  const {
    data: promotionRisk,
    isLoading: promotionRiskLoading,
    isFetched: promotionRiskFetched,
  } = useQuery({
    queryKey: insightQueryKeys.promotionRisk(insightScope, lookbackDays),
    queryFn: () => fetchPromotionRiskInsights(insightScope, lookbackDays, 20),
    enabled: activeTab === "promotionRisk",
    staleTime: 2 * 60_000,
  });

  const {
    data: reorderRisk,
    isLoading: reorderRiskLoading,
    isFetched: reorderRiskFetched,
  } = useQuery({
    queryKey: insightQueryKeys.reorderRisk(insightScope, lookbackDays),
    queryFn: () => fetchReorderRiskInsights(insightScope, lookbackDays, 20),
    enabled: activeTab === "reorderRisk",
    staleTime: 2 * 60_000,
  });

  const {
    data: trendSummary,
    isLoading: trendSummaryLoading,
    isFetched: trendSummaryFetched,
  } = useQuery({
    queryKey: insightQueryKeys.trendSummary(insightScope, lookbackDays),
    queryFn: () => fetchTrendSummaryInsights(insightScope, lookbackDays, 20),
    enabled: activeTab === "trend",
    staleTime: 2 * 60_000,
  });

  const {
    data: brokenSizeRuns,
    isLoading: brokenSizeRunsLoading,
    isFetched: brokenSizeRunsFetched,
  } = useQuery({
    queryKey: insightQueryKeys.brokenSizeRuns(insightScope, lookbackDays),
    queryFn: () => fetchBrokenSizeRuns(insightScope, lookbackDays, 20),
    enabled: activeTab === "brokenSize",
    staleTime: 2 * 60_000,
  });

  const {
    data: seasonClearance,
    isLoading: seasonClearanceLoading,
    isFetched: seasonClearanceFetched,
  } = useQuery({
    queryKey: insightQueryKeys.seasonClearance(insightScope, lookbackDays, daysWithoutOutbound),
    queryFn: () =>
      fetchSeasonClearanceInsights(insightScope, lookbackDays, daysWithoutOutbound, 20),
    enabled: activeTab === "seasonClearance",
    staleTime: 2 * 60_000,
  });

  const createTransferMutation = useMutation({
    mutationFn: createTransferFromSuggestion,
    onSuccess: (doc) => {
      setExecutingActionKey(null);
      void queryClient.invalidateQueries({ queryKey: ["inventory-documents"] });
      void recordInsightAction({
        insightKind: "transfer",
        actionCode: "transfer_execute",
        actionStatus: 4,
        resultEntityId: doc.id,
        resultEntityType: "inventory_document",
      }).catch(() => undefined);
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
      void recordInsightAction({
        insightKind: "transfer",
        actionCode: "transfer_execute",
        actionStatus: 4,
        resultEntityId: doc.id,
        resultEntityType: "inventory_document",
      }).catch(() => undefined);
      router.push(`/inventory-documents/${doc.id}`);
    },
    onError: (error) => {
      setExecutingActionKey(null);
      notifyError(error);
    },
  });

  const bulkTransferMutation = useMutation({
    mutationFn: createBulkTransfersFromInsights,
    onSuccess: (result) => {
      setExecutingActionKey(null);
      setSelectedTransferKeys([]);
      void queryClient.invalidateQueries({ queryKey: ["inventory-documents"] });
      if (result.documents[0]) {
        router.push(`/inventory-documents/${result.documents[0].documentId}`);
      }
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
    const markdownItems = markdownCandidates ?? [];
    const promotionItems = promotionRisk ?? [];
    const reorderItems = reorderRisk ?? [];
    const trendItems = trendSummary ?? [];
    const brokenSizeItems = brokenSizeRuns ?? [];
    const seasonClearanceItems = seasonClearance ?? [];

    return {
      deadCount: executiveSummary ? executiveSummary.deadStockCount : null,
      tiedCapital: executiveSummary ? executiveSummary.tiedCapital : null,
      urgentVelocity: salesVelocityFetched
        ? velocityItems.filter(
            (item) => item.severity === "critical" || item.severity === "warning"
          ).length
        : null,
      criticalDead: deadStockFetched
        ? deadItems.filter((item) => item.severity === "critical").length
        : null,
      transferCount: executiveSummary ? executiveSummary.transferOpportunityCount : transferSuggestionsFetched ? transferItems.length : null,
      markdownCount: executiveSummary ? executiveSummary.markdownCandidateCount : markdownCandidatesFetched ? markdownItems.length : null,
      promotionRiskCount: executiveSummary ? executiveSummary.promotionRiskCount : promotionRiskFetched ? promotionItems.length : null,
      reorderRiskCount: executiveSummary ? executiveSummary.reorderRiskCount : reorderRiskFetched ? reorderItems.length : null,
      trendCount: trendSummaryFetched ? trendItems.length : null,
      brokenSizeCount: brokenSizeRunsFetched ? brokenSizeItems.length : null,
      seasonClearanceCount: seasonClearanceFetched ? seasonClearanceItems.length : null,
    };
  }, [
    executiveSummary,
    deadStock,
    salesVelocity,
    transferSuggestions,
    markdownCandidates,
    promotionRisk,
    reorderRisk,
    trendSummary,
    brokenSizeRuns,
    seasonClearance,
    deadStockFetched,
    salesVelocityFetched,
    transferSuggestionsFetched,
    markdownCandidatesFetched,
    promotionRiskFetched,
    reorderRiskFetched,
    trendSummaryFetched,
    brokenSizeRunsFetched,
    seasonClearanceFetched,
  ]);

  const activeTabLoading =
    (activeTab === "deadStock" && deadStockLoading) ||
    (activeTab === "velocity" && salesVelocityLoading) ||
    (activeTab === "transfer" && transferSuggestionsLoading) ||
    (activeTab === "markdown" && markdownCandidatesLoading) ||
    (activeTab === "promotionRisk" && promotionRiskLoading) ||
    (activeTab === "reorderRisk" && reorderRiskLoading) ||
    (activeTab === "trend" && trendSummaryLoading) ||
    (activeTab === "brokenSize" && brokenSizeRunsLoading) ||
    (activeTab === "seasonClearance" && seasonClearanceLoading);

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
  const markdownChart = useMemo(() => buildMarkdownChart(markdownCandidates), [markdownCandidates]);
  const promotionChart = useMemo(() => buildPromotionChart(promotionRisk), [promotionRisk]);
  const reorderChart = useMemo(() => buildReorderChart(reorderRisk), [reorderRisk]);
  const trendChart = useMemo(() => buildTrendChart(trendSummary), [trendSummary]);

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
      {
        id: "markdown" as const,
        label: t("tabs.markdown"),
        icon: BadgeDollarSign,
        count: stats.markdownCount,
        description: t("tabHints.markdown"),
      },
      {
        id: "promotionRisk" as const,
        label: t("tabs.promotionRisk"),
        icon: Percent,
        count: stats.promotionRiskCount,
        description: t("tabHints.promotionRisk"),
      },
      {
        id: "reorderRisk" as const,
        label: t("tabs.reorderRisk"),
        icon: ShoppingCart,
        count: stats.reorderRiskCount,
        description: t("tabHints.reorderRisk"),
      },
      {
        id: "trend" as const,
        label: t("tabs.trend"),
        icon: TrendingUpDown,
        count: stats.trendCount,
        description: t("tabHints.trend"),
      },
      {
        id: "brokenSize" as const,
        label: t("tabs.brokenSize"),
        icon: Layers,
        count: stats.brokenSizeCount,
        description: t("tabHints.brokenSize"),
      },
      {
        id: "seasonClearance" as const,
        label: t("tabs.seasonClearance"),
        icon: Snowflake,
        count: stats.seasonClearanceCount,
        description: t("tabHints.seasonClearance"),
      },
    ],
    [
      t,
      stats.deadCount,
      stats.urgentVelocity,
      stats.transferCount,
      stats.markdownCount,
      stats.promotionRiskCount,
      stats.reorderRiskCount,
      stats.trendCount,
      stats.brokenSizeCount,
      stats.seasonClearanceCount,
    ]
  );

  const handleTabChange = (tab: InsightTab) => {
    setActiveTab(tab);
    router.replace(`/insights?tab=${tab}`, { scroll: false });
  };

  const handleResetFilters = () => {
    setWarehouseId(canSelectAllWarehouses ? "" : defaultWarehouseId);
    setBrandId("");
    setDaysWithoutOutbound(60);
    setLookbackDays(30);
  };

  useEffect(() => {
    if (!warehouseId && defaultWarehouseId) {
      setWarehouseId(defaultWarehouseId);
    }
  }, [defaultWarehouseId, warehouseId]);

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

      void recordInsightAction({
        insightKind: activeTab,
        actionCode: recommendation.actionCode,
        actionStatus: 1,
        payload: recommendation.params,
      }).catch(() => undefined);
    },
    [t, tActions, activeTab]
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

  const toggleTransferSelection = (rowKey: string) => {
    setSelectedTransferKeys((current) =>
      current.includes(rowKey) ? current.filter((key) => key !== rowKey) : [...current, rowKey]
    );
  };

  const handleBulkTransfer = () => {
    const lines = (transferSuggestions ?? [])
      .map((item, index) => ({
        item,
        rowKey: `${item.productVariantId}-${item.sourceWarehouseId}-${item.destinationWarehouseId}-${index}`,
      }))
      .filter(({ rowKey }) => selectedTransferKeys.includes(rowKey))
      .map(({ item }) => ({
        productVariantId: item.productVariantId,
        sourceWarehouseId: item.sourceWarehouseId,
        destinationWarehouseId: item.destinationWarehouseId,
        quantity: item.suggestedQuantity,
        sku: item.sku,
      }));

    if (!lines.length) {
      return;
    }

    setExecutingActionKey("bulk-transfer");
    bulkTransferMutation.mutate({
      note: "[INSIGHT] Bulk transfer from suggestions",
      lines,
    });
  };

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      <InsightsHeroBanner
        activeTabLabel={tabs.find((tab) => tab.id === activeTab)?.label ?? t("title")}
        summary={summaryText}
        loading={activeTabLoading}
      />

      <InsightsExecutiveSummaryStrip
        summary={executiveSummary}
        locale={locale}
      />

      <div className="card overflow-hidden">
        <InsightFilters
          activeTab={activeTab}
          warehouseId={warehouseId}
          onWarehouseChange={setWarehouseId}
          brandId={brandId}
          onBrandChange={setBrandId}
          daysWithoutOutbound={daysWithoutOutbound}
          onDaysWithoutOutboundChange={setDaysWithoutOutbound}
          lookbackDays={lookbackDays}
          onLookbackDaysChange={setLookbackDays}
          onReset={handleResetFilters}
        />

        <InsightTabBar tabs={tabs} activeTab={activeTab} onChange={handleTabChange} />

        <InsightTabContextBanner
          tab={tabs.find((tab) => tab.id === activeTab)}
          loading={activeTabLoading}
        />

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
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("deadStock.days")}</th>
                  <th>{tStocks("onHand")}</th>
                  <th>{t("deadStock.costValue")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {deadStockLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
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
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
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
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("salesVelocity.outbound")}</th>
                  <th>{t("salesVelocity.avgDaily")}</th>
                  <th>{t("salesVelocity.coverDays")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {salesVelocityLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
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
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
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
            {selectedTransferKeys.length > 0 ? (
              <div className="mb-3 flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3">
                <p className="text-sm text-slate-600">
                  {t("transfer.selectedCount", { count: selectedTransferKeys.length })}
                </p>
                <button
                  type="button"
                  className="btn-primary"
                  disabled={bulkTransferMutation.isPending}
                  onClick={handleBulkTransfer}
                >
                  {bulkTransferMutation.isPending
                    ? t("transfer.bulkCreating")
                    : t("transfer.bulkCreate")}
                </button>
              </div>
            ) : null}
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th className="w-10">
                    <span className="sr-only">{t("transfer.selectAll")}</span>
                  </th>
                  <th>{tStocks("sku")}</th>
                  <th>{t("transfer.route")}</th>
                  <th>{t("transfer.qty")}</th>
                  <th>{t("transfer.coverDays")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {transferSuggestionsLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
                ) : transferSuggestions?.length ? (
                  transferSuggestions.map((item, index) => {
                    const rowKey = `${item.productVariantId}-${item.sourceWarehouseId}-${item.destinationWarehouseId}-${index}`;
                    const recommendation = resolveRecommendation(item);
                    const executingActionId = executingActionKey?.startsWith(`${rowKey}:`)
                      ? executingActionKey.split(":").pop() ?? null
                      : null;
                    const isSelected = selectedTransferKeys.includes(rowKey);

                    return (
                      <tr key={rowKey}>
                        <td>
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => toggleTransferSelection(rowKey)}
                            aria-label={item.sku}
                          />
                        </td>
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
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
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
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}

        {activeTab === "markdown" && (
          <InsightSection
            title={t("markdown.title")}
            subtitle={t("markdown.subtitle")}
            chartTitle={t("charts.markdownValue")}
            accent="amber"
            icon={BadgeDollarSign}
            loading={markdownCandidatesLoading}
            chart={
              <MiniBarChart
                items={markdownChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
          >
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("deadStock.days")}</th>
                  <th>{t("deadStock.costValue")}</th>
                  <th>{t("markdown.depth")}</th>
                  <th>{t("markdown.recovery")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {markdownCandidatesLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
                ) : markdownCandidates?.length ? (
                  markdownCandidates.map((item) => {
                    const recommendation = resolveRecommendation(item);
                    return (
                      <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                        <td><InsightSkuCell sku={item.sku} severity={item.severity} /></td>
                        <td><InsightWarehouseCell code={item.warehouseCode} name={item.warehouseName} /></td>
                        <td className="tabular-nums">{formatNumber(item.daysWithoutOutbound, locale)}</td>
                        <td className="tabular-nums">{formatNumber(item.estimatedInventoryValue ?? 0, locale)}</td>
                        <td className="tabular-nums">{formatNumber(item.markdownDepthPercent ?? 0, locale)}%</td>
                        <td className="tabular-nums">{formatNumber(item.estimatedRecoveryValue ?? 0, locale)}</td>
                        <td>
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
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
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}

        {activeTab === "promotionRisk" && (
          <InsightSection
            title={t("promotionRisk.title")}
            subtitle={t("promotionRisk.subtitle")}
            chartTitle={t("charts.promotionRisk")}
            accent="fuchsia"
            icon={Percent}
            loading={promotionRiskLoading}
            chart={
              <MiniBarChart
                items={promotionChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
          >
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("salesVelocity.coverDays")}</th>
                  <th>{t("promotionRisk.regularPrice")}</th>
                  <th>{t("promotionRisk.promoPrice")}</th>
                  <th>{t("promotionRisk.discount")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {promotionRiskLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
                ) : promotionRisk?.length ? (
                  promotionRisk.map((item) => {
                    const recommendation = resolveRecommendation(item);
                    return (
                      <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                        <td><InsightSkuCell sku={item.sku} severity={item.severity} /></td>
                        <td><InsightWarehouseCell code={item.warehouseCode} name={item.warehouseName} /></td>
                        <td className="tabular-nums">{item.estimatedDaysOfCover != null ? formatNumber(item.estimatedDaysOfCover, locale) : "—"}</td>
                        <td className="tabular-nums">{item.regularPriceAfterVat != null ? formatNumber(item.regularPriceAfterVat, locale) : "—"}</td>
                        <td className="tabular-nums">{item.promotionPriceAfterVat != null ? formatNumber(item.promotionPriceAfterVat, locale) : "—"}</td>
                        <td className="tabular-nums">{item.promotionDiscountPercent != null ? `${formatNumber(item.promotionDiscountPercent, locale)}%` : "—"}</td>
                        <td>
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
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
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}

        {activeTab === "reorderRisk" && (
          <InsightSection
            title={t("reorderRisk.title")}
            subtitle={t("reorderRisk.subtitle")}
            chartTitle={t("charts.reorderRisk")}
            accent="emerald"
            icon={ShoppingCart}
            loading={reorderRiskLoading}
            chart={
              <MiniBarChart
                items={reorderChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
          >
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("salesVelocity.avgDaily")}</th>
                  <th>{t("salesVelocity.coverDays")}</th>
                  <th>{t("reorderRisk.onOrder")}</th>
                  <th>{t("reorderRisk.suggestedQty")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {reorderRiskLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
                ) : reorderRisk?.length ? (
                  reorderRisk.map((item) => {
                    const recommendation = resolveRecommendation(item);
                    return (
                      <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                        <td><InsightSkuCell sku={item.sku} severity={item.severity} /></td>
                        <td><InsightWarehouseCell code={item.warehouseCode} name={item.warehouseName} /></td>
                        <td className="tabular-nums">{formatNumber(item.averageDailyOutbound, locale)}</td>
                        <td className="tabular-nums">{item.estimatedDaysOfCover != null ? formatNumber(item.estimatedDaysOfCover, locale) : "—"}</td>
                        <td className="tabular-nums">{formatNumber(item.quantityOnOrder, locale)}</td>
                        <td className="tabular-nums">{item.suggestedReorderQuantity != null ? formatNumber(item.suggestedReorderQuantity, locale) : "—"}</td>
                        <td>
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
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
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}

        {activeTab === "trend" && (
          <InsightSection
            title={t("trend.title")}
            subtitle={t("trend.subtitle")}
            chartTitle={t("charts.trendDelta")}
            accent="slate"
            icon={TrendingUpDown}
            loading={trendSummaryLoading}
            chart={
              <MiniBarChart
                items={trendChart}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
          >
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("trend.inventoryDelta")}</th>
                  <th>{t("trend.outboundTrend")}</th>
                  <th>{t("trend.priceTrend")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {trendSummaryLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
                ) : trendSummary?.length ? (
                  trendSummary.map((item) => {
                    const recommendation = resolveRecommendation(item);
                    return (
                      <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                        <td><InsightSkuCell sku={item.sku} severity={item.severity} /></td>
                        <td><InsightWarehouseCell code={item.warehouseCode} name={item.warehouseName} /></td>
                        <td className="tabular-nums">{formatNumber(item.inventoryValueDelta, locale)}</td>
                        <td className="tabular-nums">{formatNumber(item.outboundTrendPercent, locale)}%</td>
                        <td className="tabular-nums">{item.priceTrendPercent != null ? `${formatNumber(item.priceTrendPercent, locale)}%` : "—"}</td>
                        <td>
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
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
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}

        {activeTab === "brokenSize" && (
          <InsightSection
            title={t("brokenSize.title")}
            subtitle={t("brokenSize.subtitle")}
            chartTitle={t("brokenSize.missingSizes")}
            accent="violet"
            icon={Layers}
            loading={brokenSizeRunsLoading}
            chart={
              <MiniBarChart
                items={buildBrokenSizeChart(brokenSizeRuns)}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
          >
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th>{t("brokenSize.product")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("brokenSize.sizesInStock")}</th>
                  <th>{t("brokenSize.missingSizes")}</th>
                  <th>{t("brokenSize.totalOnHand")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {brokenSizeRunsLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
                ) : brokenSizeRuns?.length ? (
                  brokenSizeRuns.map((item) => {
                    const resolved = item.recommendation;
                    return (
                      <tr key={`${item.productId}-${item.warehouseId}-${item.color ?? "_"}`}>
                        <td>
                          <div className="font-medium text-slate-900">{item.productName}</div>
                          {item.color ? (
                            <div className="text-xs text-slate-500">{item.color}</div>
                          ) : null}
                        </td>
                        <td>
                          <InsightWarehouseCell
                            code={item.warehouseCode}
                            name={item.warehouseName}
                          />
                        </td>
                        <td>{item.sizesInStock.join(", ")}</td>
                        <td className="text-rose-700">{item.missingSizes.join(", ")}</td>
                        <td className="tabular-nums">{formatNumber(item.totalOnHand, locale)}</td>
                        <td>
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
                            recommendation={resolved}
                            severity={item.severity}
                            locale={locale}
                            onExplain={() =>
                              openExplain(resolved, item.severity, {
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
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </InsightSection>
        )}

        {activeTab === "seasonClearance" && (
          <InsightSection
            title={t("seasonClearance.title")}
            subtitle={t("seasonClearance.subtitle")}
            chartTitle={t("seasonClearance.days")}
            accent="orange"
            icon={Snowflake}
            loading={seasonClearanceLoading}
            chart={
              <MiniBarChart
                items={buildSeasonClearanceChart(seasonClearance)}
                locale={locale}
                emptyLabel={tCommon("noData")}
                valueLabel={(value) => formatNumber(value, locale)}
              />
            }
          >
            <table className="data-table insights-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("seasonClearance.season")}</th>
                  <th>{t("seasonClearance.days")}</th>
                  <th>{t("seasonClearance.suggestedPrice")}</th>
                  <th className="w-28">{t("severity.label")}</th>
                  <th className="min-w-[14rem]">{t("recommendation.label")}</th>
                </tr>
              </thead>
              <tbody>
                {seasonClearanceLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
                ) : seasonClearance?.length ? (
                  seasonClearance.map((item) => {
                    const resolved = item.recommendation;
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
                        <td>{item.season ?? "—"}</td>
                        <td className="tabular-nums">{formatNumber(item.daysWithoutOutbound, locale)}</td>
                        <td className="tabular-nums">
                          {item.suggestedMarkdownPriceAfterVat != null
                            ? formatNumber(item.suggestedMarkdownPriceAfterVat, locale)
                            : "—"}
                        </td>
                        <td>
                          <InsightPriorityCell severity={item.severity} />
                        </td>
                        <td>
                          <RecommendationCard
                            compact
                            recommendation={resolved}
                            severity={item.severity}
                            locale={locale}
                            onExplain={() =>
                              openExplain(resolved, item.severity, {
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
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
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

function buildMarkdownChart(items: MarkdownCandidateInsight[] | undefined) {
  if (!Array.isArray(items) || !items.length) return [];
  return [...items]
    .sort((a, b) => (b.estimatedInventoryValue ?? 0) - (a.estimatedInventoryValue ?? 0))
    .slice(0, TOP_CHART_COUNT)
    .map((item) => ({
      id: `${item.productVariantId}-${item.warehouseId}`,
      label: item.sku,
      sublabel: item.warehouseCode,
      value: item.estimatedInventoryValue ?? 0,
      severity: item.severity,
    }));
}

function buildPromotionChart(items: PromotionRiskInsight[] | undefined) {
  if (!Array.isArray(items) || !items.length) return [];
  return [...items]
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

function buildReorderChart(items: ReorderRiskInsight[] | undefined) {
  if (!Array.isArray(items) || !items.length) return [];
  return [...items]
    .sort((a, b) => (b.suggestedReorderQuantity ?? 0) - (a.suggestedReorderQuantity ?? 0))
    .slice(0, TOP_CHART_COUNT)
    .map((item) => ({
      id: `${item.productVariantId}-${item.warehouseId}`,
      label: item.sku,
      sublabel: item.warehouseCode,
      value: item.suggestedReorderQuantity ?? 0,
      severity: item.severity,
    }));
}

function buildTrendChart(items: TrendSummaryInsight[] | undefined) {
  if (!Array.isArray(items) || !items.length) return [];
  return [...items]
    .sort((a, b) => Math.abs(b.inventoryValueDelta) - Math.abs(a.inventoryValueDelta))
    .slice(0, TOP_CHART_COUNT)
    .map((item) => ({
      id: `${item.productVariantId}-${item.warehouseId}`,
      label: item.sku,
      sublabel: item.warehouseCode,
      value: Math.abs(item.inventoryValueDelta),
      severity: item.severity,
    }));
}

function buildBrokenSizeChart(items: BrokenSizeRunInsight[] | undefined) {
  if (!Array.isArray(items) || !items.length) return [];
  return [...items]
    .sort((a, b) => b.sizesWithoutStock - a.sizesWithoutStock)
    .slice(0, TOP_CHART_COUNT)
    .map((item) => ({
      id: `${item.productId}-${item.warehouseId}`,
      label: item.productName,
      sublabel: item.warehouseCode,
      value: item.sizesWithoutStock,
      severity: item.severity,
    }));
}

function buildSeasonClearanceChart(items: SeasonClearanceInsight[] | undefined) {
  if (!Array.isArray(items) || !items.length) return [];
  return [...items]
    .sort((a, b) => b.daysWithoutOutbound - a.daysWithoutOutbound)
    .slice(0, TOP_CHART_COUNT)
    .map((item) => ({
      id: `${item.productVariantId}-${item.warehouseId}`,
      label: item.sku,
      sublabel: item.season ?? item.warehouseCode,
      value: item.daysWithoutOutbound,
      severity: item.severity,
    }));
}
