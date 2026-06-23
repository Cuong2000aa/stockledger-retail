"use client";

import { Search, X } from "lucide-react";
import { useTranslations } from "next-intl";

type ListFilterBarProps = {
  search: string;
  onSearchChange: (value: string) => void;
  searchPlaceholder?: string;
  onReset?: () => void;
  showReset?: boolean;
  children?: React.ReactNode;
};

export function ListFilterBar({
  search,
  onSearchChange,
  searchPlaceholder,
  onReset,
  showReset,
  children,
}: ListFilterBarProps) {
  const t = useTranslations("filters");
  const tCommon = useTranslations("common");
  const canReset = showReset ?? Boolean(search || children);

  return (
    <div className="card mb-5 p-4 shadow-card">
      <div className="flex flex-wrap items-end gap-3">
        <label className="min-w-[220px] flex-1 text-sm text-slate-600">
          <span className="mb-1 block">{tCommon("search")}</span>
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
