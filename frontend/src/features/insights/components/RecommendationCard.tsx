"use client";

import { Link } from "@/i18n/routing";
import { SeverityBadge } from "@/features/insights/components/SeverityBadge";
import {
  buildNavigateHref,
} from "@/features/insights/recommendation-utils";
import type { InsightRecommendation } from "@/lib/types";
import { formatNumber } from "@/lib/format";
import clsx from "clsx";
import {
  ArrowRightLeft,
  Eye,
  FileOutput,
  Loader2,
  PackageSearch,
  ShoppingCart,
  Sparkles,
  TrendingUp,
} from "lucide-react";
import { useTranslations } from "next-intl";
import type { LucideIcon } from "lucide-react";

const actionTypeStyles = {
  monitor: {
    icon: TrendingUp,
    ring: "ring-sky-200",
    bg: "bg-gradient-to-br from-sky-50 to-white",
    accent: "text-sky-600",
  },
  review: {
    icon: PackageSearch,
    ring: "ring-slate-200",
    bg: "bg-gradient-to-br from-slate-50 to-white",
    accent: "text-slate-600",
  },
  markdown: {
    icon: FileOutput,
    ring: "ring-amber-200",
    bg: "bg-gradient-to-br from-amber-50 to-white",
    accent: "text-amber-700",
  },
  replenish: {
    icon: ShoppingCart,
    ring: "ring-indigo-200",
    bg: "bg-gradient-to-br from-indigo-50 to-white",
    accent: "text-indigo-700",
  },
  transfer: {
    icon: ArrowRightLeft,
    ring: "ring-violet-200",
    bg: "bg-gradient-to-br from-violet-50 to-white",
    accent: "text-violet-700",
  },
} as const;

export function RecommendationCard({
  recommendation,
  severity,
  locale,
  executingActionId,
  onApiAction,
}: {
  recommendation?: InsightRecommendation;
  severity: string;
  locale: string;
  executingActionId?: string | null;
  onApiAction?: (actionId: string) => void;
}) {
  const t = useTranslations("insights");
  const tActions = useTranslations("insights.actions");
  const tCtas = useTranslations("insights.recommendation.ctas");

  if (!recommendation?.actionCode) {
    return <span className="text-slate-400">—</span>;
  }

  const style = actionTypeStyles[recommendation.actionType] ?? actionTypeStyles.review;
  const Icon = style.icon;

  let title = recommendation.titleKey;
  try {
    title = t(`recommendation.titles.${recommendation.titleKey}` as never);
  } catch {
    // keep key
  }

  let detail = "";
  try {
    detail = tActions(recommendation.actionCode as never, recommendation.params ?? {});
  } catch {
    detail = recommendation.actionCode;
  }

  const evidenceEntries = Object.entries(recommendation.evidence ?? {}).filter(
    ([key]) => key !== "sku" && key !== "warehouseCode"
  );

  const sortedActions = [...(recommendation.actions ?? [])].sort(
    (a, b) => Number(b.isPrimary) - Number(a.isPrimary)
  );

  return (
    <div
      className={clsx(
        "min-w-[18rem] rounded-xl border p-3 ring-1",
        style.bg,
        style.ring
      )}
    >
      <div className="mb-2 flex items-start gap-2.5">
        <div
          className={clsx(
            "flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-white shadow-sm ring-1 ring-inset",
            style.ring
          )}
        >
          <Icon className={clsx("h-4 w-4", style.accent)} />
        </div>
        <div className="min-w-0 flex-1">
          <div className="mb-1 flex flex-wrap items-center gap-1.5">
            <p className="text-sm font-semibold leading-snug text-slate-900">{title}</p>
            <SeverityBadge severity={severity} label={severityLabel(t, severity)} />
            <span className="rounded-full bg-white/80 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-slate-500 ring-1 ring-slate-200">
              P{recommendation.priority}
            </span>
          </div>
          <p className="text-xs leading-relaxed text-slate-600">{detail}</p>
        </div>
      </div>

      {evidenceEntries.length > 0 && (
        <div className="mb-2.5 flex flex-wrap gap-1.5">
          {evidenceEntries.map(([key, value]) => (
            <EvidenceChip
              key={key}
              label={evidenceLabel(t, key)}
              value={formatEvidenceValue(key, value, locale, t)}
            />
          ))}
        </div>
      )}

      {sortedActions.length > 0 && (
        <div className="flex flex-wrap gap-1.5 border-t border-white/80 pt-2.5">
          {sortedActions.map((action) => {
            const label = ctaLabel(tCtas, action.labelKey);
            const isExecuting = executingActionId === action.id;

            if (action.kind === "api") {
              return (
                <button
                  key={action.id}
                  type="button"
                  className={ctaClass(action.isPrimary)}
                  disabled={isExecuting}
                  onClick={() => onApiAction?.(action.id)}
                >
                  {isExecuting ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Sparkles className="h-3.5 w-3.5" />
                  )}
                  {label}
                </button>
              );
            }

            const href = buildNavigateHref(action.route ?? "/", action.payload ?? {});

            return (
              <Link
                key={action.id}
                href={href}
                className={ctaClass(action.isPrimary)}
              >
                <CtaIcon labelKey={action.labelKey} />
                {label}
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}

function EvidenceChip({ label, value }: { label: string; value: string }) {
  return (
    <span className="inline-flex items-center gap-1 rounded-lg bg-white/90 px-2 py-1 text-[11px] ring-1 ring-slate-200/80">
      <span className="font-medium text-slate-500">{label}</span>
      <span className="font-semibold tabular-nums text-slate-800">{value}</span>
    </span>
  );
}

function CtaIcon({ labelKey }: { labelKey: string }) {
  const icons: Record<string, LucideIcon> = {
    view_stock: Eye,
    draft_transfer: ArrowRightLeft,
    draft_po: ShoppingCart,
    draft_stock_out: FileOutput,
    review_dead_stock: PackageSearch,
    preview_transfer: ArrowRightLeft,
    create_transfer: Sparkles,
  };
  const Icon = icons[labelKey] ?? ArrowRightLeft;
  return <Icon className="h-3.5 w-3.5" />;
}

function ctaClass(isPrimary: boolean) {
  return clsx(
    "inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-xs font-semibold transition-all",
    isPrimary
      ? "bg-slate-900 text-white hover:bg-slate-800"
      : "bg-white text-slate-700 ring-1 ring-slate-200 hover:bg-slate-50"
  );
}

function ctaLabel(
  translate: ReturnType<typeof useTranslations<"insights.recommendation.ctas">>,
  labelKey: string
) {
  try {
    return translate(labelKey as never);
  } catch {
    return labelKey;
  }
}

function severityLabel(
  t: ReturnType<typeof useTranslations<"insights">>,
  severity: string
) {
  if (severity === "critical") return t("severity.critical");
  if (severity === "warning") return t("severity.warning");
  return t("severity.info");
}

function evidenceLabel(t: ReturnType<typeof useTranslations<"insights">>, key: string) {
  try {
    return t(`recommendation.evidence.${key}` as never);
  } catch {
    return key;
  }
}

function formatEvidenceValue(
  key: string,
  value: string,
  locale: string,
  t: ReturnType<typeof useTranslations<"insights">>
) {
  if (key === "transferBlocked") {
    try {
      return t(`recommendation.blocked.${value}` as never);
    } catch {
      return value;
    }
  }
  if (key === "costValue" || key === "onHand" || key === "outboundQty" || key === "quantity") {
    return formatNumber(Number(value), locale);
  }
  if (key === "avgDaily" || key === "coverDays" || key === "destCoverDays") {
    return formatNumber(Number(value), locale);
  }
  if (key === "daysIdle") {
    return `${formatNumber(Number(value), locale)}d`;
  }
  return value;
}
