"use client";

import { formatNumber } from "@/lib/format";
import {
  recalcFromVatChange,
  recalcPriceAfterVat,
  recalcPriceBeforeVat,
} from "@/lib/pricing";
import { PriceType, type ProductPrice } from "@/lib/types";
import { useLocale, useTranslations } from "next-intl";

export type PriceForm = {
  priceBeforeVat: string;
  vatRate: string;
  priceAfterVat: string;
  effectiveFrom: string;
  effectiveTo: string;
};

export const emptyPriceForm = (): PriceForm => ({
  priceBeforeVat: "",
  vatRate: "",
  priceAfterVat: "",
  effectiveFrom: new Date().toISOString().slice(0, 10),
  effectiveTo: "",
});

function PriceHistoryList({ items }: { items: ProductPrice[] }) {
  const t = useTranslations("variants");
  const tCommon = useTranslations("common");
  const locale = useLocale();

  if (items.length === 0) {
    return <div className="text-xs text-slate-400">{tCommon("noData")}</div>;
  }

  return (
    <div className="space-y-2">
      {items.slice(0, 4).map((item) => (
        <div
          key={item.id}
          className="rounded-xl bg-white px-3 py-2 text-xs text-slate-700 ring-1 ring-slate-100"
        >
          <div className="flex items-center justify-between gap-3">
            <span className="font-medium text-slate-900">
              {formatNumber(item.priceAfterVat, locale)} {item.currency}
            </span>
            <span className="text-slate-500">
              {item.isCurrent ? t("currentPriceBadge") : t("historyPriceBadge")}
            </span>
          </div>
          <div className="mt-1 text-slate-500">
            {t("sellingPriceBeforeVat")}: {formatNumber(item.priceBeforeVat, locale)} | {t("vatRate")}:{" "}
            {formatNumber(item.vatRate, locale)}%
          </div>
          <div className="mt-1 text-slate-500">
            {item.effectiveFrom.slice(0, 10)}
            {item.effectiveTo ? ` → ${item.effectiveTo.slice(0, 10)}` : ` → ${t("openEnded")}`}
          </div>
        </div>
      ))}
    </div>
  );
}

type PriceEditorCardProps = {
  title: string;
  description: string;
  badge?: string;
  formState: PriceForm;
  setFormState: (value: PriceForm) => void;
  priceType: PriceType;
  history: ProductPrice[];
  canSavePrice: boolean;
  savePending: boolean;
  onSave: (priceType: PriceType, form: PriceForm) => void;
};

export function PriceEditorCard({
  title,
  description,
  badge,
  formState,
  setFormState,
  priceType,
  history,
  canSavePrice,
  savePending,
  onSave,
}: PriceEditorCardProps) {
  const t = useTranslations("variants");

  return (
    <div className="rounded-2xl border border-slate-100 bg-slate-50/60 p-4">
      <div className="mb-3 flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-semibold text-slate-900">{title}</p>
          <p className="mt-1 text-xs text-slate-500">{description}</p>
        </div>
        {badge && (
          <span className="rounded-full bg-white px-2.5 py-1 text-[11px] font-medium text-slate-500 ring-1 ring-slate-200">
            {badge}
          </span>
        )}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="mb-1 block text-sm">{t("sellingPriceBeforeVat")}</label>
          <input
            type="text"
            inputMode="decimal"
            className="input"
            value={formState.priceBeforeVat}
            onChange={(e) => {
              const next = {
                ...formState,
                priceBeforeVat: e.target.value,
                priceAfterVat: recalcPriceAfterVat({
                  priceBeforeVat: e.target.value,
                  vatRate: formState.vatRate,
                }),
              };
              setFormState(next);
            }}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm">{t("vatRate")}</label>
          <input
            type="text"
            inputMode="decimal"
            className="input"
            value={formState.vatRate}
            onChange={(e) => {
              const recalced = recalcFromVatChange({
                ...formState,
                vatRate: e.target.value,
              });
              setFormState({
                ...formState,
                vatRate: e.target.value,
                ...recalced,
              });
            }}
          />
        </div>
      </div>
      <div className="mt-3 grid grid-cols-2 gap-3">
        <div>
          <label className="mb-1 block text-sm">{t("sellingPriceAfterVat")}</label>
          <input
            type="text"
            inputMode="decimal"
            className="input"
            value={formState.priceAfterVat}
            onChange={(e) => {
              const next = {
                ...formState,
                priceAfterVat: e.target.value,
                priceBeforeVat: recalcPriceBeforeVat({
                  priceAfterVat: e.target.value,
                  vatRate: formState.vatRate,
                }),
              };
              setFormState(next);
            }}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm">{t("effectiveFrom")}</label>
          <input
            type="date"
            className="input"
            value={formState.effectiveFrom}
            onChange={(e) => setFormState({ ...formState, effectiveFrom: e.target.value })}
          />
        </div>
      </div>
      <div className="mt-3">
        <label className="mb-1 block text-sm">{t("effectiveTo")}</label>
        <input
          type="date"
          className="input"
          value={formState.effectiveTo}
          onChange={(e) => setFormState({ ...formState, effectiveTo: e.target.value })}
        />
      </div>
      {canSavePrice ? (
        <button
          type="button"
          className="btn-secondary mt-3"
          disabled={savePending}
          onClick={() => onSave(priceType, formState)}
        >
          {t("savePrice")}
        </button>
      ) : (
        <div className="mt-3 rounded-xl bg-white px-3 py-2 text-xs text-slate-500 ring-1 ring-slate-100">
          {t("createSkuFirst")}
        </div>
      )}

      <div className="mt-4">
        <p className="mb-2 text-xs font-semibold uppercase text-slate-500">{t("priceHistory")}</p>
        <PriceHistoryList items={history} />
      </div>
    </div>
  );
}
