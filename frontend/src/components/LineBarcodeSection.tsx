"use client";

import { UnitBarcodesInput } from "@/components/UnitBarcodesInput";
import { variantRequiresBarcode } from "@/lib/variantBarcode";
import type { ProductVariant } from "@/lib/types";
import { Info } from "lucide-react";
import { useTranslations } from "next-intl";

type LineBarcodeSectionProps = {
  productVariantId: string;
  variant: ProductVariant | undefined;
  quantity: number;
  value: string;
  onChange: (value: string) => void;
  poBarcodesRemaining?: string[];
  onApplyPoBarcodes?: () => void;
};

export function LineBarcodeSection({
  productVariantId,
  variant,
  quantity,
  value,
  onChange,
  poBarcodesRemaining,
  onApplyPoBarcodes,
}: LineBarcodeSectionProps) {
  const t = useTranslations("documents");
  const tCommon = useTranslations("common");

  if (!productVariantId) {
    return null;
  }

  if (!variant) {
    return (
      <p className="text-xs text-slate-400">{tCommon("loading")}</p>
    );
  }

  if (!variantRequiresBarcode(variant)) {
    return (
      <div className="flex items-start gap-2 rounded-xl border border-slate-100 bg-slate-50/80 px-3 py-2.5 text-xs text-slate-500">
        <Info className="mt-0.5 h-3.5 w-3.5 shrink-0 text-slate-400" />
        <span>{t("skuNoUnitBarcode")}</span>
      </div>
    );
  }

  const hasPoSuggestion =
    (poBarcodesRemaining?.length ?? 0) > 0 && onApplyPoBarcodes;

  return (
    <div className="rounded-xl border border-violet-100 bg-violet-50/40 p-3">
      {hasPoSuggestion && (
        <div className="mb-3 rounded-lg border border-violet-200/80 bg-white/70 px-3 py-2.5">
          <p className="text-xs font-medium text-violet-900">
            {t("poBarcodesHint")}
          </p>
          <p className="mt-1 font-mono text-xs leading-relaxed text-slate-600 break-all">
            {poBarcodesRemaining!.join(", ")}
          </p>
          <button
            type="button"
            className="mt-2 text-xs font-semibold text-violet-700 hover:text-violet-900 hover:underline"
            onClick={onApplyPoBarcodes}
          >
            {t("applyPoBarcodes")}
          </button>
        </div>
      )}
      <UnitBarcodesInput
        quantity={quantity}
        value={value}
        onChange={onChange}
        className="w-full"
      />
    </div>
  );
}
