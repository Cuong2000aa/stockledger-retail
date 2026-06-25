import clsx from "clsx";
import type { LucideIcon } from "lucide-react";

type FormSectionProps = {
  title: string;
  description?: string;
  icon?: LucideIcon;
  action?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
};

export function FormSection({
  title,
  description,
  icon: Icon,
  action,
  children,
  className,
}: FormSectionProps) {
  return (
    <section className={clsx("rounded-xl border border-slate-200/80 bg-white", className)}>
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-slate-100 px-4 py-3.5 sm:px-5">
        <div className="flex min-w-0 items-start gap-2.5">
          {Icon && (
            <span className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
              <Icon className="h-4 w-4" />
            </span>
          )}
          <div>
            <h2 className="text-sm font-semibold text-slate-900">{title}</h2>
            {description && (
              <p className="mt-0.5 text-xs text-slate-500">{description}</p>
            )}
          </div>
        </div>
        {action}
      </div>
      <div className="p-4 sm:p-5">{children}</div>
    </section>
  );
}
