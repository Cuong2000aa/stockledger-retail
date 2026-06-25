"use client";

import type { InsightTab } from "../types";
import clsx from "clsx";
import { Bot, Sparkles } from "lucide-react";
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
        "mb-6 overflow-hidden rounded-2xl bg-gradient-to-r p-[1px] ring-1",
        gradients[activeTab]
      )}
    >
      <div className="rounded-[calc(1rem-1px)] bg-white/90 px-5 py-4 backdrop-blur-sm">
        <div className="flex flex-wrap items-start gap-4">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-violet-500 to-indigo-600 text-white shadow-md shadow-violet-500/30">
            <Bot className="h-5 w-5" />
          </div>
          <div className="min-w-0 flex-1">
            <div className="mb-1 flex flex-wrap items-center gap-2">
              <p className="text-sm font-semibold text-slate-900">{t("copilot.bannerTitle")}</p>
              <span className="inline-flex items-center gap-1 rounded-full bg-violet-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-violet-700 ring-1 ring-violet-200">
                <Sparkles className="h-3 w-3" />
                {t("aiReady")}
              </span>
            </div>
            {loading ? (
              <div className="space-y-2">
                <div className="skeleton h-3 w-full max-w-2xl" />
                <div className="skeleton h-3 w-4/5 max-w-xl" />
              </div>
            ) : (
              <p className="text-sm leading-relaxed text-slate-600">{summary}</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
