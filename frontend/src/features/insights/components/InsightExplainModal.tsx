"use client";

import { FormModal } from "@/components/FormModal";
import { SeverityBadge } from "@/features/insights/components/SeverityBadge";
import {
  buildInsightExplanation,
} from "@/features/insights/insight-explain";
import type { InsightExplainContext } from "@/features/insights/types";
import type { InsightRecommendation } from "@/lib/types";
import { Bot, Sparkles } from "lucide-react";
import { useTranslations } from "next-intl";

export function InsightExplainModal({
  open,
  onClose,
  recommendation,
  severity,
  actionDetail,
  title,
  context,
}: {
  open: boolean;
  onClose: () => void;
  recommendation?: InsightRecommendation;
  severity: string;
  actionDetail: string;
  title: string;
  context: InsightExplainContext;
}) {
  const t = useTranslations("insights");
  const tDialog = useTranslations("dialog");

  const { paragraphs, evidenceLines } = buildInsightExplanation(
    recommendation,
    context,
    actionDetail,
    (key, values) => {
      try {
        return t(key as never, values as never);
      } catch {
        return key;
      }
    }
  );

  return (
    <FormModal
      open={open}
      title={t("copilot.explainTitle")}
      onClose={onClose}
      size="lg"
      footer={
        <button type="button" className="btn-primary" onClick={onClose}>
          {tDialog("ok")}
        </button>
      }
    >
      <div className="space-y-4">
        <div className="flex items-start gap-3 rounded-xl bg-gradient-to-br from-violet-50 to-indigo-50/50 p-4 ring-1 ring-violet-100">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white text-violet-600 shadow-sm ring-1 ring-violet-100">
            <Bot className="h-5 w-5" />
          </span>
          <div className="min-w-0 flex-1">
            <div className="mb-1 flex flex-wrap items-center gap-2">
              <p className="font-semibold text-slate-900">{title}</p>
              <SeverityBadge
                severity={severity}
                label={
                  severity === "critical"
                    ? t("severity.critical")
                    : severity === "warning"
                      ? t("severity.warning")
                      : t("severity.info")
                }
              />
            </div>
            <p className="text-xs text-violet-700/80">{t("copilot.explainSubtitle")}</p>
          </div>
        </div>

        <div className="space-y-3 text-sm leading-relaxed text-slate-700">
          {paragraphs.map((paragraph, index) => (
            <p key={index}>{paragraph}</p>
          ))}
        </div>

        {evidenceLines.length > 0 ? (
          <div className="rounded-xl border border-slate-100 bg-slate-50/80 p-3">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
              {t("copilot.evidenceTitle")}
            </p>
            <ul className="space-y-1 text-xs text-slate-600">
              {evidenceLines.map((line) => (
                <li key={line} className="font-mono">
                  {line}
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        <p className="flex items-center gap-1.5 text-[11px] text-slate-400">
          <Sparkles className="h-3.5 w-3.5 shrink-0" />
          {t("copilot.futureAiNote")}
        </p>
      </div>
    </FormModal>
  );
}
