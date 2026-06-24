import type { InsightExplainContext, InsightTab } from "@/features/insights/types";
import type { InsightRecommendation } from "@/lib/types";

type TranslateInsights = (key: string, values?: Record<string, string | number>) => string;

export function getInsightSummaryKey(
  tab: InsightTab,
  stats: {
    deadCount: number | null;
    tiedCapital: number | null;
    urgentVelocity: number | null;
    transferCount: number | null;
    criticalDead: number | null;
  }
): { key: string; values?: Record<string, string | number> } {
  if (tab === "deadStock") {
    return {
      key: stats.deadCount ? "copilot.summaryDeadStock" : "copilot.summaryDeadStockEmpty",
      values: {
        count: stats.deadCount ?? 0,
        capital: stats.tiedCapital ?? 0,
        critical: stats.criticalDead ?? 0,
      },
    };
  }
  if (tab === "velocity") {
    return {
      key: stats.urgentVelocity ? "copilot.summaryVelocity" : "copilot.summaryVelocityEmpty",
      values: { alerts: stats.urgentVelocity ?? 0 },
    };
  }
  return {
    key: stats.transferCount ? "copilot.summaryTransfer" : "copilot.summaryTransferEmpty",
    values: { count: stats.transferCount ?? 0 },
  };
}

export function getInsightRationaleKey(
  recommendation: InsightRecommendation | undefined
): string | null {
  if (!recommendation?.actionCode) {
    return null;
  }

  const code = recommendation.actionCode;
  if (code.startsWith("dead_stock_critical")) return "copilot.rationale.deadCritical";
  if (code.startsWith("dead_stock_markdown")) return "copilot.rationale.deadMarkdown";
  if (code.startsWith("dead_stock")) return "copilot.rationale.deadReview";
  if (code.startsWith("velocity_replenish_urgent")) return "copilot.rationale.velocityUrgent";
  if (code.startsWith("velocity_replenish")) return "copilot.rationale.velocityPlan";
  if (code.startsWith("velocity_no_demand")) return "copilot.rationale.velocityNoDemand";
  if (code.startsWith("velocity_monitor")) return "copilot.rationale.velocityMonitor";
  if (code.startsWith("transfer_execute")) return "copilot.rationale.transfer";
  return null;
}

export function buildInsightExplanation(
  recommendation: InsightRecommendation | undefined,
  context: InsightExplainContext,
  actionDetail: string,
  t: TranslateInsights
): { paragraphs: string[]; evidenceLines: string[] } {
  if (!recommendation?.actionCode) {
    return { paragraphs: [], evidenceLines: [] };
  }

  const location = context.warehouseCode
    ? t("copilot.locationSkuWarehouse", {
        sku: context.sku ?? "SKU",
        warehouse: context.warehouseCode,
      })
    : context.sourceWarehouseCode && context.destinationWarehouseCode
      ? t("copilot.locationTransfer", {
          sku: context.sku ?? "SKU",
          from: context.sourceWarehouseCode,
          to: context.destinationWarehouseCode,
        })
      : context.sku ?? t("copilot.locationItem");

  const paragraphs: string[] = [
    t("copilot.explainIntro", {
      location,
      priority: recommendation.priority,
    }),
    actionDetail,
  ];

  const rationaleKey = getInsightRationaleKey(recommendation);
  if (rationaleKey) {
    try {
      paragraphs.push(t(rationaleKey));
    } catch {
      // skip missing key
    }
  }

  const evidenceLines = Object.entries(recommendation.evidence ?? {})
    .filter(([key]) => key !== "sku" && key !== "warehouseCode")
    .map(([key, value]) => {
      let label = key;
      try {
        label = t(`recommendation.evidence.${key}` as never);
      } catch {
        // keep key
      }
      return `${label}: ${value}`;
    });

  return { paragraphs, evidenceLines };
}
