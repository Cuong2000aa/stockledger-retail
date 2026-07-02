"use client";

import { SeverityBadge } from "@/features/insights/components/SeverityBadge";
import type { InsightTabConfig } from "@/features/insights/components/InsightTabBar";
import clsx from "clsx";
import type { LucideIcon } from "lucide-react";
import { useTranslations } from "next-intl";

const panelAccents: Record<
  string,
  { ring: string; bg: string; icon: string }
> = {
  deadStock: {
    ring: "ring-rose-200",
    bg: "from-rose-50 to-white",
    icon: "bg-rose-600 text-white",
  },
  velocity: {
    ring: "ring-indigo-200",
    bg: "from-indigo-50 to-white",
    icon: "bg-indigo-600 text-white",
  },
  transfer: {
    ring: "ring-sky-200",
    bg: "from-sky-50 to-white",
    icon: "bg-sky-600 text-white",
  },
  markdown: {
    ring: "ring-amber-200",
    bg: "from-amber-50 to-white",
    icon: "bg-amber-600 text-white",
  },
  promotionRisk: {
    ring: "ring-fuchsia-200",
    bg: "from-fuchsia-50 to-white",
    icon: "bg-fuchsia-600 text-white",
  },
  reorderRisk: {
    ring: "ring-emerald-200",
    bg: "from-emerald-50 to-white",
    icon: "bg-emerald-600 text-white",
  },
  trend: {
    ring: "ring-slate-200",
    bg: "from-slate-50 to-white",
    icon: "bg-slate-700 text-white",
  },
  brokenSize: {
    ring: "ring-violet-200",
    bg: "from-violet-50 to-white",
    icon: "bg-violet-600 text-white",
  },
  seasonClearance: {
    ring: "ring-orange-200",
    bg: "from-orange-50 to-white",
    icon: "bg-orange-600 text-white",
  },
};

export function InsightTabContextBanner({
  tab,
  loading,
}: {
  tab: InsightTabConfig | undefined;
  loading?: boolean;
}) {
  const t = useTranslations("insights");

  if (!tab) {
    return null;
  }

  const accent = panelAccents[tab.id] ?? panelAccents.trend;
  const Icon = tab.icon as LucideIcon;

  return (
    <div
      className={clsx(
        "border-b border-slate-100 bg-gradient-to-r px-5 py-4 ring-1 ring-inset",
        accent.bg,
        accent.ring
      )}
    >
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="flex min-w-0 items-start gap-3">
          <span
            className={clsx(
              "flex h-11 w-11 shrink-0 items-center justify-center rounded-xl shadow-sm",
              accent.icon
            )}
          >
            <Icon className="h-5 w-5" />
          </span>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-lg font-bold text-slate-900">{tab.label}</h2>
              <span className="rounded-full bg-white px-2.5 py-0.5 text-xs font-bold tabular-nums text-slate-700 ring-1 ring-slate-200">
                {loading ? "…" : tab.count == null ? "—" : tab.count}{" "}
                {t("panel.results")}
              </span>
            </div>
            <p className="mt-1 max-w-3xl text-sm leading-relaxed text-slate-600">
              {tab.description}
            </p>
          </div>
        </div>

        <div className="shrink-0 rounded-xl bg-white/80 px-3 py-2.5 ring-1 ring-slate-200/80">
          <p className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-slate-500">
            {t("panel.priorityLegend")}
          </p>
          <div className="flex flex-wrap gap-2">
            <SeverityBadge severity="critical" label={t("severity.critical")} />
            <SeverityBadge severity="warning" label={t("severity.warning")} />
            <SeverityBadge severity="info" label={t("severity.info")} />
          </div>
        </div>
      </div>
    </div>
  );
}
