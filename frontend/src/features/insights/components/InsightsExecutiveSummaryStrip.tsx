"use client";

import { StatCard } from "@/components/StatCard";
import type { InsightsExecutiveSummary } from "@/features/insights/types";
import { formatNumber } from "@/lib/format";
import {
  AlertTriangle,
  ArrowRightLeft,
  BadgeDollarSign,
  LineChart,
  PackageX,
  ShoppingCart,
} from "lucide-react";
import { useTranslations } from "next-intl";

export function InsightsExecutiveSummaryStrip({
  summary,
  locale,
}: {
  summary?: InsightsExecutiveSummary;
  locale: string;
}) {
  const t = useTranslations("insights.stats");

  return (
    <div className="mb-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
      <StatCard
        label={t("deadSkus")}
        value={summary ? formatNumber(summary.deadStockCount, locale) : "—"}
        icon={PackageX}
        accent="rose"
      />
      <StatCard
        label={t("tiedCapital")}
        value={summary ? formatNumber(summary.tiedCapital, locale) : "—"}
        icon={BadgeDollarSign}
        accent="amber"
      />
      <StatCard
        label={t("marginAtRisk")}
        value={summary ? formatNumber(summary.marginAtRisk, locale) : "—"}
        icon={LineChart}
        accent="rose"
      />
      <StatCard
        label={t("promotionRisk")}
        value={summary ? formatNumber(summary.promotionRiskCount, locale) : "—"}
        icon={AlertTriangle}
        accent="indigo"
      />
      <StatCard
        label={t("reorderRisk")}
        value={summary ? formatNumber(summary.reorderRiskCount, locale) : "—"}
        icon={ShoppingCart}
        accent="emerald"
      />
      <StatCard
        label={t("transferValue")}
        value={summary ? formatNumber(summary.transferOpportunityValue, locale) : "—"}
        icon={ArrowRightLeft}
        accent="sky"
      />
    </div>
  );
}
