"use client";

import type { InsightTab } from "../types";
import clsx from "clsx";
import type { LucideIcon } from "lucide-react";

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
    active: "bg-rose-600 text-white shadow-sm",
    idle: "bg-rose-50 text-rose-800 hover:bg-rose-100 ring-1 ring-rose-200/80",
    badge: "bg-white/25 text-white",
  },
  velocity: {
    active: "bg-indigo-600 text-white shadow-sm",
    idle: "bg-indigo-50 text-indigo-800 hover:bg-indigo-100 ring-1 ring-indigo-200/80",
    badge: "bg-white/25 text-white",
  },
  transfer: {
    active: "bg-sky-600 text-white shadow-sm",
    idle: "bg-sky-50 text-sky-800 hover:bg-sky-100 ring-1 ring-sky-200/80",
    badge: "bg-white/25 text-white",
  },
  markdown: {
    active: "bg-amber-600 text-white shadow-sm",
    idle: "bg-amber-50 text-amber-800 hover:bg-amber-100 ring-1 ring-amber-200/80",
    badge: "bg-white/25 text-white",
  },
  promotionRisk: {
    active: "bg-fuchsia-600 text-white shadow-sm",
    idle: "bg-fuchsia-50 text-fuchsia-800 hover:bg-fuchsia-100 ring-1 ring-fuchsia-200/80",
    badge: "bg-white/25 text-white",
  },
  reorderRisk: {
    active: "bg-emerald-600 text-white shadow-sm",
    idle: "bg-emerald-50 text-emerald-800 hover:bg-emerald-100 ring-1 ring-emerald-200/80",
    badge: "bg-white/25 text-white",
  },
  trend: {
    active: "bg-slate-700 text-white shadow-sm",
    idle: "bg-slate-50 text-slate-800 hover:bg-slate-100 ring-1 ring-slate-200/80",
    badge: "bg-white/25 text-white",
  },
};

export function InsightTabBar({
  tabs,
  activeTab,
  onChange,
}: {
  tabs: InsightTabConfig[];
  activeTab: InsightTab;
  onChange: (tab: InsightTab) => void;
}) {
  const activeDescription = tabs.find((tab) => tab.id === activeTab)?.description;

  return (
    <div className="border-b border-slate-100 bg-white px-4 py-3">
      <div className="flex gap-2 overflow-x-auto pb-1">
        {tabs.map((tab) => {
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
                "inline-flex shrink-0 items-center gap-2 rounded-xl px-3 py-2 text-sm font-semibold transition-colors",
                isActive ? accent.active : accent.idle
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              <span className="whitespace-nowrap">{tab.label}</span>
              <span
                className={clsx(
                  "rounded-full px-1.5 py-0.5 text-xs font-bold tabular-nums",
                  isActive
                    ? accent.badge
                    : "bg-white text-slate-700 ring-1 ring-slate-200"
                )}
              >
                {tab.count == null ? "—" : tab.count}
              </span>
            </button>
          );
        })}
      </div>
      {activeDescription ? (
        <p className="mt-2 text-xs leading-relaxed text-slate-500">{activeDescription}</p>
      ) : null}
    </div>
  );
}
