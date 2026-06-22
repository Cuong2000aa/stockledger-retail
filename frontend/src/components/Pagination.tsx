"use client";

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
    <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3 text-sm text-slate-600">
      <span>
        {t("total")}: {totalCount} {t("items")} — {t("page")} {page} {t("of")}{" "}
        {totalPages}
      </span>
      <div className="flex gap-2">
        <button
          className="btn-secondary disabled:opacity-40"
          disabled={page <= 1}
          onClick={() => onChange(page - 1)}
        >
          ←
        </button>
        <button
          className="btn-secondary disabled:opacity-40"
          disabled={page >= totalPages}
          onClick={() => onChange(page + 1)}
        >
          →
        </button>
      </div>
    </div>
  );
}
