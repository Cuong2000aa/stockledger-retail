"use client";

import { Trash2 } from "lucide-react";

type DocumentLineCardProps = {
  index: number;
  canRemove?: boolean;
  onRemove?: () => void;
  children: React.ReactNode;
};

export function DocumentLineCard({
  index,
  canRemove,
  onRemove,
  children,
}: DocumentLineCardProps) {
  return (
    <div className="rounded-xl border border-slate-200/90 bg-gradient-to-b from-slate-50/80 to-white p-4 shadow-sm transition-shadow hover:shadow-md">
      <div className="mb-3 flex items-center justify-between gap-2">
        <span
          className="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-600 text-xs font-bold text-white shadow-sm"
          aria-label={`Line ${index}`}
        >
          {index}
        </span>
        {canRemove && onRemove && (
          <button
            type="button"
            className="btn-ghost text-red-600 hover:bg-red-50 hover:text-red-700"
            onClick={onRemove}
          >
            <Trash2 className="h-4 w-4" />
          </button>
        )}
      </div>
      <div className="space-y-3">{children}</div>
    </div>
  );
}
