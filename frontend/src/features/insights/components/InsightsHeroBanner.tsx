"use client";

import type { InsightTab } from "../types";
import clsx from "clsx";
import { Bot } from "lucide-react";
import { useTranslations } from "next-intl";

export function InsightsHeroBanner({
  activeTab,
  summary,
  loading,
}: {
  activeTab: InsightTab;
  summary: string;
  loading?: boolean;
}) {
  const t = useTranslations("insights");

  const gradients: Record<InsightTab, string> = {
    deadStock: "from-rose-500/15 via-amber-500/10 to-violet-500/15 ring-rose-200/60",
    velocity: "from-indigo-500/15 via-violet-500/10 to-sky-500/15 ring-indigo-200/60",
    transfer: "from-sky-500/15 via-cyan-500/10 to-violet-500/15 ring-sky-200/60",
    markdown: "from-amber-500/15 via-orange-500/10 to-rose-500/15 ring-amber-200/60",
    promotionRisk: "from-fuchsia-500/15 via-pink-500/10 to-rose-500/15 ring-fuchsia-200/60",
    reorderRisk: "from-emerald-500/15 via-green-500/10 to-lime-500/15 ring-emerald-200/60",
    trend: "from-slate-500/15 via-slate-400/10 to-indigo-500/15 ring-slate-200/60",
  };

  return (
    <div
      className={clsx(
        "mb-4 overflow-hidden rounded-xl bg-gradient-to-r p-px ring-1",
        gradients[activeTab]
      )}
    >
      <div className="rounded-[calc(0.75rem-1px)] bg-white/95 px-4 py-3 backdrop-blur-sm">
        <div className="flex items-start gap-3">
          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-violet-600 text-white">
            <Bot className="h-4 w-4" />
          </div>
          <div className="min-w-0 flex-1">
            <p className="mb-0.5 text-xs font-semibold text-slate-700">{t("copilot.bannerTitle")}</p>
            {loading ? (
              <div className="skeleton h-3 w-full max-w-xl" />
            ) : (
              <p className="line-clamp-2 text-sm leading-snug text-slate-600">{summary}</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
