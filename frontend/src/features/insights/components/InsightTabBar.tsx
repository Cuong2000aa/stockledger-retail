"use client";

import type { InsightTab } from "../types";
import clsx from "clsx";
import type { LucideIcon } from "lucide-react";
import { useTranslations } from "next-intl";

export type InsightTabConfig = {
  id: InsightTab;
  label: string;
  icon: LucideIcon;
  count: number | null;
  description: string;
};

const tabAccents: Record<
  InsightTab,
  { active: string; idle: string; badge: string }
> = {
  deadStock: {
    active: "bg-rose-600 text-white shadow-md ring-2 ring-rose-300/50",
    idle: "bg-white text-rose-800 hover:bg-rose-50 ring-1 ring-rose-200",
    badge: "bg-white/25 text-white",
  },
  velocity: {
    active: "bg-indigo-600 text-white shadow-md ring-2 ring-indigo-300/50",
    idle: "bg-white text-indigo-800 hover:bg-indigo-50 ring-1 ring-indigo-200",
    badge: "bg-white/25 text-white",
  },
  transfer: {
    active: "bg-sky-600 text-white shadow-md ring-2 ring-sky-300/50",
    idle: "bg-white text-sky-800 hover:bg-sky-50 ring-1 ring-sky-200",
    badge: "bg-white/25 text-white",
  },
  markdown: {
    active: "bg-amber-600 text-white shadow-md ring-2 ring-amber-300/50",
    idle: "bg-white text-amber-800 hover:bg-amber-50 ring-1 ring-amber-200",
    badge: "bg-white/25 text-white",
  },
  promotionRisk: {
    active: "bg-fuchsia-600 text-white shadow-md ring-2 ring-fuchsia-300/50",
    idle: "bg-white text-fuchsia-800 hover:bg-fuchsia-50 ring-1 ring-fuchsia-200",
    badge: "bg-white/25 text-white",
  },
  reorderRisk: {
    active: "bg-emerald-600 text-white shadow-md ring-2 ring-emerald-300/50",
    idle: "bg-white text-emerald-800 hover:bg-emerald-50 ring-1 ring-emerald-200",
    badge: "bg-white/25 text-white",
  },
  trend: {
    active: "bg-slate-700 text-white shadow-md ring-2 ring-slate-400/50",
    idle: "bg-white text-slate-800 hover:bg-slate-50 ring-1 ring-slate-200",
    badge: "bg-white/25 text-white",
  },
  brokenSize: {
    active: "bg-violet-600 text-white shadow-md ring-2 ring-violet-300/50",
    idle: "bg-white text-violet-800 hover:bg-violet-50 ring-1 ring-violet-200",
    badge: "bg-white/25 text-white",
  },
  seasonClearance: {
    active: "bg-orange-600 text-white shadow-md ring-2 ring-orange-300/50",
    idle: "bg-white text-orange-800 hover:bg-orange-50 ring-1 ring-orange-200",
    badge: "bg-white/25 text-white",
  },
};

const OPERATIONS_TABS: InsightTab[] = [
  "deadStock",
  "velocity",
  "transfer",
  "markdown",
  "promotionRisk",
  "reorderRisk",
  "trend",
];

const FASHION_TABS: InsightTab[] = ["brokenSize", "seasonClearance"];

function TabGroup({
  label,
  tabs,
  allTabs,
  activeTab,
  onChange,
}: {
  label: string;
  tabs: InsightTab[];
  allTabs: InsightTabConfig[];
  activeTab: InsightTab;
  onChange: (tab: InsightTab) => void;
}) {
  const items = allTabs.filter((tab) => tabs.includes(tab.id));

  if (!items.length) {
    return null;
  }

  return (
    <div>
      <p className="mb-2 text-[11px] font-bold uppercase tracking-wider text-slate-400">
        {label}
      </p>
      <div className="flex flex-wrap gap-2">
        {items.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          const accent = tabAccents[tab.id];

          return (
            <button
              key={tab.id}
              type="button"
              title={tab.description}
              onClick={() => onChange(tab.id)}
              className={clsx(
                "inline-flex shrink-0 items-center gap-2 rounded-xl px-3.5 py-2.5 text-sm font-semibold transition-all",
                isActive ? accent.active : accent.idle
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              <span className="whitespace-nowrap">{tab.label}</span>
              <span
                className={clsx(
                  "min-w-[1.5rem] rounded-full px-1.5 py-0.5 text-center text-xs font-bold tabular-nums",
                  isActive
                    ? accent.badge
                    : "bg-slate-100 text-slate-700 ring-1 ring-slate-200"
                )}
              >
                {tab.count == null ? "—" : tab.count}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}

export function InsightTabBar({
  tabs,
  activeTab,
  onChange,
}: {
  tabs: InsightTabConfig[];
  activeTab: InsightTab;
  onChange: (tab: InsightTab) => void;
}) {
  const t = useTranslations("insights.tabs");

  return (
    <div className="border-b border-slate-200 bg-slate-50/80 px-4 py-4">
      <div className="space-y-4">
        <TabGroup
          label={t("groupOperations")}
          tabs={OPERATIONS_TABS}
          allTabs={tabs}
          activeTab={activeTab}
          onChange={onChange}
        />
        <TabGroup
          label={t("groupFashion")}
          tabs={FASHION_TABS}
          allTabs={tabs}
          activeTab={activeTab}
          onChange={onChange}
        />
      </div>
    </div>
  );
}
