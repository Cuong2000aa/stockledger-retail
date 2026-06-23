"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { useTranslations } from "next-intl";

export function Pagination({
  page,
  pageSize,
  totalCount,
  onChange,
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  onChange: (page: number) => void;
}) {
  const t = useTranslations("common");
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 bg-slate-50/50 px-5 py-3.5 text-sm text-slate-600">
      <span className="font-medium">
        {t("total")}: <span className="text-slate-900">{totalCount}</span> {t("items")} —{" "}
        {t("page")} <span className="text-slate-900">{page}</span> {t("of")}{" "}
        <span className="text-slate-900">{totalPages}</span>
      </span>
      <div className="flex gap-2">
        <button
          className="btn-secondary !px-3 !py-2"
          disabled={page <= 1}
          onClick={() => onChange(page - 1)}
          aria-label="Previous page"
        >
          <ChevronLeft className="h-4 w-4" />
        </button>
        <button
          className="btn-secondary !px-3 !py-2"
          disabled={page >= totalPages}
          onClick={() => onChange(page + 1)}
          aria-label="Next page"
        >
          <ChevronRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
