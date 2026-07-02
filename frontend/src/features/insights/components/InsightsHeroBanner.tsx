"use client";

import type { InsightTab } from "../types";
import clsx from "clsx";
import { Bot } from "lucide-react";
import { useTranslations } from "next-intl";

export function InsightsHeroBanner({
  activeTabLabel,
  summary,
  loading,
}: {
  activeTabLabel: string;
  summary: string;
  loading?: boolean;
}) {
  const t = useTranslations("insights");

  return (
    <div className="mb-5 overflow-hidden rounded-2xl border border-violet-200/80 bg-gradient-to-r from-violet-50 via-white to-indigo-50 shadow-sm">
      <div className="px-5 py-4">
        <div className="flex items-start gap-4">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-violet-600 text-white shadow-sm">
            <Bot className="h-5 w-5" />
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-xs font-bold uppercase tracking-wide text-violet-700">
              {t("copilot.bannerTitle")}
            </p>
            <p className="mt-1 text-sm font-semibold text-slate-800">
              {activeTabLabel}
            </p>
            {loading ? (
              <div className="skeleton mt-2 h-4 w-full max-w-2xl rounded" />
            ) : (
              <p className="mt-2 text-sm leading-relaxed text-slate-600">{summary}</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
