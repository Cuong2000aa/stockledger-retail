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
  { active: string; idle: string; badge: string; ring: string }
> = {
  deadStock: {
    active: "bg-gradient-to-r from-rose-600 to-rose-500 text-white shadow-lg shadow-rose-500/25",
    idle: "bg-rose-50/80 text-rose-800 hover:bg-rose-100",
    badge: "bg-white/25 text-white",
    ring: "ring-rose-200/80",
  },
  velocity: {
    active: "bg-gradient-to-r from-indigo-600 to-violet-500 text-white shadow-lg shadow-indigo-500/25",
    idle: "bg-indigo-50/80 text-indigo-800 hover:bg-indigo-100",
    badge: "bg-white/25 text-white",
    ring: "ring-indigo-200/80",
  },
  transfer: {
    active: "bg-gradient-to-r from-sky-600 to-cyan-500 text-white shadow-lg shadow-sky-500/25",
    idle: "bg-sky-50/80 text-sky-800 hover:bg-sky-100",
    badge: "bg-white/25 text-white",
    ring: "ring-sky-200/80",
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
  return (
    <div className="border-b border-slate-100 bg-white px-4 py-3">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-stretch">
        {tabs.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          const accent = tabAccents[tab.id];

          return (
            <button
              key={tab.id}
              type="button"
              onClick={() => onChange(tab.id)}
              className={clsx(
                "group flex min-w-0 flex-1 flex-col rounded-2xl px-4 py-3 text-left transition-all duration-200 ring-1",
                isActive ? accent.active : clsx(accent.idle, accent.ring)
              )}
            >
              <div className="flex items-center gap-2">
                <span
                  className={clsx(
                    "flex h-8 w-8 shrink-0 items-center justify-center rounded-xl transition-colors",
                    isActive ? "bg-white/20" : "bg-white ring-1 ring-black/5"
                  )}
                >
                  <Icon className="h-4 w-4" />
                </span>
                <span className="min-w-0 flex-1 truncate text-sm font-semibold">{tab.label}</span>
                <span
                  className={clsx(
                    "shrink-0 rounded-full px-2 py-0.5 text-xs font-bold tabular-nums",
                    isActive
                      ? accent.badge
                      : "bg-white text-slate-700 ring-1 ring-slate-200"
                  )}
                >
                  {tab.count == null ? "—" : tab.count}
                </span>
              </div>
              <p
                className={clsx(
                  "mt-1.5 line-clamp-2 text-[11px] leading-relaxed",
                  isActive ? "text-white/85" : "text-slate-500"
                )}
              >
                {tab.description}
              </p>
            </button>
          );
        })}
      </div>
    </div>
  );
}
