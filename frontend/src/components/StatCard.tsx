import type { LucideIcon } from "lucide-react";
import clsx from "clsx";

const accentStyles = {
  indigo: "from-indigo-500/10 to-indigo-600/5 text-indigo-600 ring-indigo-500/20",
  emerald: "from-emerald-500/10 to-emerald-600/5 text-emerald-600 ring-emerald-500/20",
  sky: "from-sky-500/10 to-sky-600/5 text-sky-600 ring-sky-500/20",
  amber: "from-amber-500/10 to-amber-600/5 text-amber-600 ring-amber-500/20",
  rose: "from-rose-500/10 to-rose-600/5 text-rose-600 ring-rose-500/20",
} as const;

type Accent = keyof typeof accentStyles;

export function StatCard({
  label,
  value,
  icon: Icon,
  accent = "indigo",
}: {
  label: string;
  value: string;
  icon: LucideIcon;
  accent?: Accent;
}) {
  return (
    <div className="card group p-5 transition-all duration-200 hover:shadow-card-hover">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
            {label}
          </p>
          <p className="mt-2 text-2xl font-bold tracking-tight text-slate-900">
            {value}
          </p>
        </div>
        <div
          className={clsx(
            "flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br ring-1",
            accentStyles[accent]
          )}
        >
          <Icon className="h-5 w-5" />
        </div>
      </div>
    </div>
  );
}
