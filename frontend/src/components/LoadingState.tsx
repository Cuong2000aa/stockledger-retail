"use client";

import { Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";

export function LoadingSpinner({ className }: { className?: string }) {
  return (
    <Loader2
      className={`h-5 w-5 animate-spin text-brand-600 ${className ?? ""}`}
      aria-hidden
    />
  );
}

export function PageLoading() {
  const t = useTranslations("common");

  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-slate-500">
      <LoadingSpinner className="h-8 w-8" />
      <p className="text-sm font-medium">{t("loading")}</p>
    </div>
  );
}

export function TableSkeleton({ rows = 5, cols = 4 }: { rows?: number; cols?: number }) {
  return (
    <div className="p-4 space-y-3">
      {Array.from({ length: rows }).map((_, row) => (
        <div key={row} className="flex gap-4">
          {Array.from({ length: cols }).map((_, col) => (
            <div
              key={col}
              className="skeleton h-9 flex-1"
              style={{ opacity: 1 - row * 0.12 }}
            />
          ))}
        </div>
      ))}
    </div>
  );
}

export function StatCardsSkeleton({ count = 5 }: { count?: number }) {
  return (
    <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="card p-5">
          <div className="skeleton mb-3 h-3 w-20" />
          <div className="skeleton h-8 w-16" />
        </div>
      ))}
    </div>
  );
}
