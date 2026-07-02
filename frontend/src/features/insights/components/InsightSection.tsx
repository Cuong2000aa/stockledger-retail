"use client";

import clsx from "clsx";
import type { LucideIcon } from "lucide-react";
import { SeverityBadge } from "./SeverityBadge";
import { useTranslations } from "next-intl";

export type InsightSectionAccent =
  | "rose"
  | "indigo"
  | "sky"
  | "amber"
  | "emerald"
  | "fuchsia"
  | "slate"
  | "violet"
  | "orange";

export function InsightSection({
  title,
  subtitle,
  chartTitle,
  chart,
  loading,
  accent,
  icon: Icon,
  children,
}: {
  title: string;
  subtitle: string;
  chartTitle: string;
  chart: React.ReactNode;
  loading: boolean;
  accent: InsightSectionAccent;
  icon: LucideIcon;
  children: React.ReactNode;
}) {
  const t = useTranslations("insights");

  const accentStyles = {
    rose: {
      panel: "from-rose-50/90 to-white border-rose-100",
      icon: "bg-rose-500/10 text-rose-600 ring-rose-500/20",
    },
    indigo: {
      panel: "from-indigo-50/90 to-white border-indigo-100",
      icon: "bg-indigo-500/10 text-indigo-600 ring-indigo-500/20",
    },
    sky: {
      panel: "from-sky-50/90 to-white border-sky-100",
      icon: "bg-sky-500/10 text-sky-600 ring-sky-500/20",
    },
    amber: {
      panel: "from-amber-50/90 to-white border-amber-100",
      icon: "bg-amber-500/10 text-amber-600 ring-amber-500/20",
    },
    emerald: {
      panel: "from-emerald-50/90 to-white border-emerald-100",
      icon: "bg-emerald-500/10 text-emerald-600 ring-emerald-500/20",
    },
    fuchsia: {
      panel: "from-fuchsia-50/90 to-white border-fuchsia-100",
      icon: "bg-fuchsia-500/10 text-fuchsia-600 ring-fuchsia-500/20",
    },
    slate: {
      panel: "from-slate-50/90 to-white border-slate-200",
      icon: "bg-slate-500/10 text-slate-600 ring-slate-500/20",
    },
    violet: {
      panel: "from-violet-50/90 to-white border-violet-100",
      icon: "bg-violet-500/10 text-violet-600 ring-violet-500/20",
    },
    orange: {
      panel: "from-orange-50/90 to-white border-orange-100",
      icon: "bg-orange-500/10 text-orange-600 ring-orange-500/20",
    },
  } as const;

  const style = accentStyles[accent];

  return (
    <div className="grid gap-0 xl:grid-cols-[minmax(18rem,26rem)_1fr]">
      <div
        className={clsx(
          "border-b border-slate-100 bg-gradient-to-b p-5 xl:border-b-0 xl:border-r",
          style.panel
        )}
      >
        <div className="mb-4 flex items-start gap-3">
          <span
            className={clsx(
              "flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ring-1",
              style.icon
            )}
          >
            <Icon className="h-5 w-5" />
          </span>
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
              {t("panel.chartLabel")}
            </p>
            <h3 className="mt-0.5 text-sm font-bold text-slate-900">{chartTitle}</h3>
          </div>
        </div>
        <div className="rounded-xl bg-white p-4 ring-1 ring-slate-200/80 shadow-sm">
          {loading ? (
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, index) => (
                <div key={index} className="skeleton h-11 w-full rounded-lg" />
              ))}
            </div>
          ) : (
            chart
          )}
        </div>
      </div>

      <div className="min-w-0">
        <div className="border-b border-slate-100 bg-slate-50/50 px-5 py-3">
          <h3 className="text-sm font-bold text-slate-900">{title}</h3>
          <p className="mt-0.5 text-xs text-slate-500">{subtitle}</p>
        </div>
        <div className="table-wrap max-h-[40rem] overflow-y-auto scrollbar-thin">
          {children}
        </div>
      </div>
    </div>
  );
}

export function InsightPriorityCell({ severity }: { severity: string }) {
  const t = useTranslations("insights");

  const label =
    severity === "critical"
      ? t("severity.critical")
      : severity === "warning"
        ? t("severity.warning")
        : t("severity.info");

  return <SeverityBadge severity={severity} label={label} />;
}

export function InsightSkuCell({ sku, severity }: { sku: string; severity: string }) {
  return (
    <div className="flex items-center gap-2">
      <span className="rounded-lg bg-slate-100 px-2.5 py-1.5 font-mono text-xs font-bold text-slate-900 ring-1 ring-slate-200">
        {sku}
      </span>
    </div>
  );
}

export function InsightWarehouseCell({
  code,
  name,
}: {
  code: string;
  name?: string;
}) {
  return (
    <div>
      <p className="font-semibold text-slate-900">{code}</p>
      {name ? <p className="mt-0.5 text-xs leading-snug text-slate-500">{name}</p> : null}
    </div>
  );
}

export function InsightTransferRouteCell({
  from,
  to,
}: {
  from: string;
  to: string;
}) {
  const t = useTranslations("insights.transfer");

  return (
    <div className="inline-flex flex-col gap-1 rounded-lg bg-slate-50 px-2.5 py-2 text-xs ring-1 ring-slate-200">
      <div className="flex items-center gap-1.5">
        <span className="font-medium text-slate-500">{t("from")}</span>
        <span className="font-semibold text-slate-800">{from}</span>
      </div>
      <div className="flex items-center gap-1.5">
        <span className="font-medium text-slate-500">{t("to")}</span>
        <span className="font-semibold text-sky-700">{to}</span>
      </div>
    </div>
  );
}

export function InsightNumericCell({
  value,
  emphasize,
  suffix,
}: {
  value: string;
  emphasize?: boolean;
  suffix?: string;
}) {
  return (
    <span
      className={clsx(
        "tabular-nums",
        emphasize ? "text-base font-bold text-slate-900" : "font-medium text-slate-700"
      )}
    >
      {value}
      {suffix ? <span className="ml-0.5 text-xs font-normal text-slate-500">{suffix}</span> : null}
    </span>
  );
}

export function LoadingRow({ colSpan, label }: { colSpan: number; label: string }) {
  return (
    <tr>
      <td colSpan={colSpan} className="py-12 text-center text-slate-500">
        <span className="inline-flex items-center gap-2 text-sm">
          <span className="h-4 w-4 animate-spin rounded-full border-2 border-slate-300 border-t-brand-500" />
          {label}
        </span>
      </td>
    </tr>
  );
}

export function EmptyRow({ colSpan, label }: { colSpan: number; label: string }) {
  return (
    <tr>
      <td colSpan={colSpan} className="py-16 text-center">
        <p className="text-sm font-medium text-slate-500">{label}</p>
      </td>
    </tr>
  );
}
