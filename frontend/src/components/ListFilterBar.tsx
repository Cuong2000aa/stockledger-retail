"use client";

import { Filter, Search, X } from "lucide-react";
import { useTranslations } from "next-intl";
import clsx from "clsx";

type ListFilterBarProps = {
  search: string;
  onSearchChange: (value: string) => void;
  searchPlaceholder?: string;
  onReset?: () => void;
  showReset?: boolean;
  children?: React.ReactNode;
  variant?: "default" | "enhanced";
  title?: string;
};

export function ListFilterBar({
  search,
  onSearchChange,
  searchPlaceholder,
  onReset,
  showReset,
  children,
  variant = "default",
  title,
}: ListFilterBarProps) {
  const t = useTranslations("filters");
  const tCommon = useTranslations("common");
  const canReset = showReset ?? Boolean(search || children);
  const isEnhanced = variant === "enhanced";

  return (
    <div
      className={clsx(
        "card mb-6 overflow-hidden",
        isEnhanced ? "shadow-card" : "mb-5 p-4 shadow-card"
      )}
    >
      {isEnhanced && (
        <div className="border-b border-slate-100 bg-gradient-to-r from-slate-50 to-white px-5 py-4">
          <div className="flex items-center gap-2 text-sm font-semibold text-slate-700">
            <Filter className="h-4 w-4 text-brand-500" />
            {title ?? t("title")}
          </div>
        </div>
      )}
      <div className={clsx("flex flex-wrap items-end gap-3", isEnhanced ? "p-5" : "")}>
        <label className="min-w-[220px] flex-1 text-sm text-slate-600">
          <span
            className={clsx(
              "mb-1 block",
              isEnhanced &&
                "flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-slate-500"
            )}
          >
            {isEnhanced && <Search className="h-3.5 w-3.5" />}
            {tCommon("search")}
          </span>
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              className="input pl-9"
              value={search}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder={searchPlaceholder ?? t("searchPlaceholder")}
            />
          </div>
        </label>
        {children}
        {canReset && onReset && (
          <button type="button" className="btn-secondary" onClick={onReset}>
            <X className="mr-1 inline h-4 w-4" />
            {t("clear")}
          </button>
        )}
      </div>
    </div>
  );
}
