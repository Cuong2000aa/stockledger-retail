"use client";

import {
  formatUnitBarcodes,
  parseUnitBarcodes,
} from "@/lib/unitBarcode";
import clsx from "clsx";
import { useTranslations } from "next-intl";

type UnitBarcodesInputProps = {
  quantity: number;
  value: string;
  onChange: (value: string) => void;
  className?: string;
  hideLabel?: boolean;
};

export function UnitBarcodesInput({
  quantity,
  value,
  onChange,
  className,
  hideLabel,
}: UnitBarcodesInputProps) {
  const t = useTranslations("documents");
  const expected = Math.abs(quantity);
  const parsed = parseUnitBarcodes(value);
  const hasBarcodes = parsed.length > 0;
  const countMismatch = hasBarcodes && parsed.length !== expected;

  const handleBlur = () => {
    if (!value.trim()) {
      return;
    }
    const formatted = formatUnitBarcodes(parsed);
    if (formatted !== value) {
      onChange(formatted);
    }
  };

  return (
    <div className={className ?? "min-w-0 flex-1"}>
      {!hideLabel && (
        <label
          className={clsx(
            "mb-1.5 block text-sm font-medium",
            countMismatch ? "text-red-600" : "text-slate-700"
          )}
        >
          {t("barcode")}
          <span
            className={clsx(
              "ml-2 rounded-md px-1.5 py-0.5 text-xs font-semibold tabular-nums",
              countMismatch
                ? "bg-red-100 text-red-700"
                : "bg-slate-100 text-slate-600"
            )}
          >
            {parsed.length}/{expected}
          </span>
        </label>
      )}
      {hideLabel && (
        <div className="mb-1.5 flex justify-end">
          <span
            className={clsx(
              "rounded-md px-1.5 py-0.5 text-xs font-semibold tabular-nums",
              countMismatch
                ? "bg-red-100 text-red-700"
                : "bg-white text-slate-600 ring-1 ring-violet-100"
            )}
          >
            {parsed.length}/{expected}
          </span>
        </div>
      )}
      <input
        type="text"
        className={clsx(
          "input font-mono text-xs",
          countMismatch && "border-red-300 ring-red-200 focus:border-red-400 focus:ring-red-200"
        )}
        placeholder={t("unitBarcodesPlaceholder")}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onBlur={handleBlur}
      />
      {countMismatch ? (
        <p className="mt-1.5 text-xs text-red-600">
          {t("unitBarcodesCountMismatch", {
            expected,
            actual: parsed.length,
          })}
        </p>
      ) : (
        <p className="mt-1.5 text-xs text-slate-500">{t("unitBarcodesHint")}</p>
      )}
    </div>
  );
}
