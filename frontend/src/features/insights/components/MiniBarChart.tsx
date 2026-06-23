import clsx from "clsx";
import { formatNumber } from "@/lib/format";

export interface MiniBarItem {
  id: string;
  label: string;
  sublabel?: string;
  value: number;
  severity?: string;
}

const barColors = {
  critical: "from-red-500 to-red-400",
  warning: "from-amber-500 to-amber-400",
  info: "from-sky-500 to-sky-400",
  default: "from-brand-500 to-brand-400",
} as const;

export function MiniBarChart({
  items,
  valueLabel,
  locale,
  emptyLabel,
}: {
  items: MiniBarItem[];
  valueLabel: (value: number) => string;
  locale: string;
  emptyLabel: string;
}) {
  if (!items.length) {
    return (
      <div className="flex h-full min-h-[12rem] items-center justify-center rounded-xl border border-dashed border-slate-200 bg-slate-50/50 px-4 text-sm text-slate-500">
        {emptyLabel}
      </div>
    );
  }

  const max = Math.max(...items.map((item) => item.value), 1);

  return (
    <div className="space-y-3">
      {items.map((item, index) => {
        const width = Math.max((item.value / max) * 100, 4);
        const colorKey =
          item.severity === "critical" || item.severity === "warning" || item.severity === "info"
            ? item.severity
            : "default";

        return (
          <div key={item.id} className="group">
            <div className="mb-1.5 flex items-baseline justify-between gap-2">
              <div className="min-w-0 flex-1">
                <p className="truncate font-mono text-xs font-semibold text-slate-800">
                  {item.label}
                </p>
                {item.sublabel && (
                  <p className="truncate text-[11px] text-slate-500">{item.sublabel}</p>
                )}
              </div>
              <span className="shrink-0 text-xs font-semibold tabular-nums text-slate-700">
                {valueLabel(item.value)}
              </span>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-slate-100">
              <div
                className={clsx(
                  "h-full rounded-full bg-gradient-to-r transition-all duration-500",
                  barColors[colorKey]
                )}
                style={{ width: `${width}%` }}
                title={`#${index + 1}: ${formatNumber(item.value, locale)}`}
              />
            </div>
          </div>
        );
      })}
    </div>
  );
}
