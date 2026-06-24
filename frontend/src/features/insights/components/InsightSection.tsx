"use client";

import clsx from "clsx";
import type { LucideIcon } from "lucide-react";

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
  accent: "rose" | "indigo" | "sky";
  icon: LucideIcon;
  children: React.ReactNode;
}) {
  const accentStyles = {
    rose: {
      panel: "from-rose-50/80 to-white border-rose-100",
      icon: "bg-rose-500/10 text-rose-600 ring-rose-500/20",
    },
    indigo: {
      panel: "from-indigo-50/80 to-white border-indigo-100",
      icon: "bg-indigo-500/10 text-indigo-600 ring-indigo-500/20",
    },
    sky: {
      panel: "from-sky-50/80 to-white border-sky-100",
      icon: "bg-sky-500/10 text-sky-600 ring-sky-500/20",
    },
  } as const;

  const style = accentStyles[accent];

  return (
    <div className="grid gap-0 xl:grid-cols-[minmax(17rem,24rem)_1fr]">
      <div
        className={clsx(
          "border-b border-slate-100 bg-gradient-to-b p-5 xl:border-b-0 xl:border-r",
          style.panel
        )}
      >
        <div className="mb-4 flex items-start gap-3">
          <span
            className={clsx(
              "flex h-9 w-9 shrink-0 items-center justify-center rounded-xl ring-1",
              style.icon
            )}
          >
            <Icon className="h-4 w-4" />
          </span>
          <div>
            <h3 className="text-sm font-semibold text-slate-900">{chartTitle}</h3>
            <p className="mt-1 text-xs leading-relaxed text-slate-500">{subtitle}</p>
          </div>
        </div>
        <div className="rounded-xl bg-white/70 p-3 ring-1 ring-slate-100/80">
          {loading ? (
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, index) => (
                <div key={index} className="skeleton h-10 w-full rounded-lg" />
              ))}
            </div>
          ) : (
            chart
          )}
        </div>
      </div>

      <div className="min-w-0">
        <div className="border-b border-slate-100 bg-white px-5 py-4">
          <h2 className="font-semibold text-slate-900">{title}</h2>
          <p className="mt-1 text-sm text-slate-500">{subtitle}</p>
        </div>
        <div className="table-wrap max-h-[36rem] overflow-y-auto scrollbar-thin">{children}</div>
      </div>
    </div>
  );
}

export function InsightSkuCell({ sku, severity }: { sku: string; severity: string }) {
  const stripe =
    severity === "critical"
      ? "bg-red-500"
      : severity === "warning"
        ? "bg-amber-500"
        : "bg-sky-400";

  return (
    <div className="flex items-center gap-2">
      <span className={clsx("h-8 w-1 shrink-0 rounded-full", stripe)} aria-hidden />
      <span className="rounded-lg bg-slate-100 px-2.5 py-1 font-mono text-xs font-semibold text-slate-800">
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
      <p className="font-medium text-slate-800">{code}</p>
      {name ? <p className="text-xs text-slate-500">{name}</p> : null}
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
  return (
    <div className="inline-flex items-center gap-1.5 rounded-lg bg-slate-50 px-2 py-1 text-xs font-medium text-slate-700 ring-1 ring-slate-200">
      <span>{from}</span>
      <span className="text-slate-400">→</span>
      <span className="text-sky-700">{to}</span>
    </div>
  );
}

export function LoadingRow({ colSpan, label }: { colSpan: number; label: string }) {
  return (
    <tr>
      <td colSpan={colSpan} className="py-10 text-center text-slate-500">
        <span className="inline-flex items-center gap-2">
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
      <td colSpan={colSpan} className="py-14 text-center">
        <p className="text-sm text-slate-500">{label}</p>
      </td>
    </tr>
  );
}
