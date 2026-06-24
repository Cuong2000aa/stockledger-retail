import type { LucideIcon } from "lucide-react";
import clsx from "clsx";

type DataTableCardProps = {
  title: string;
  subtitle?: string;
  icon?: LucideIcon;
  count?: number;
  countLabel?: string;
  children: React.ReactNode;
  className?: string;
};

export function DataTableCard({
  title,
  subtitle,
  icon: Icon,
  count,
  countLabel,
  children,
  className,
}: DataTableCardProps) {
  return (
    <div className={clsx("card overflow-hidden", className)}>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 bg-gradient-to-r from-slate-50/80 to-white px-5 py-4">
        <div className="flex items-start gap-3">
          {Icon && (
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600 ring-1 ring-brand-100">
              <Icon className="h-4 w-4" />
            </div>
          )}
          <div>
            <h2 className="font-semibold text-slate-900">{title}</h2>
            {subtitle && <p className="mt-0.5 text-xs text-slate-500">{subtitle}</p>}
          </div>
        </div>
        {count != null && countLabel && (
          <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-600">
            {countLabel}: <span className="font-semibold text-slate-900">{count}</span>
          </span>
        )}
      </div>
      {children}
    </div>
  );
}

export function EmptyTableState({ message }: { message: string }) {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
      <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-slate-400">
        <span className="text-xl">—</span>
      </div>
      <p className="text-sm text-slate-500">{message}</p>
    </div>
  );
}

export function CodePill({ children }: { children: React.ReactNode }) {
  return (
    <span className="inline-flex rounded-md bg-slate-100 px-2 py-0.5 font-mono text-xs font-medium text-slate-700 ring-1 ring-slate-200/80">
      {children}
    </span>
  );
}
