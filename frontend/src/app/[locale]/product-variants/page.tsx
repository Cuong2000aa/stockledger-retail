"use client";

import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { FormModal } from "@/components/FormModal";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton, StatCardsSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
import { ActiveBadge, costSourceKey, isProductActive } from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { useNotify } from "@/hooks/useNotify";
import {
  createProductVariant,
  deleteProductVariant,
  fetchProductPrices,
  fetchProductVariants,
  fetchProducts,
  upsertProductPrice,
  updateProductVariant,
} from "@/lib/api";
import { fetchBrands } from "@/features/admin/api";
import { validateProductVariantForm } from "@/lib/validation";
import { formatNumber } from "@/lib/format";
import {
  calcPriceAfterVat,
  calcPriceBeforeVat,
  parsePriceField,
} from "@/lib/pricing";
import { CostSource, PriceType, ProductStatus, type ProductPrice, type ProductVariant } from "@/lib/types";
import {
  emptyPriceForm,
  PriceEditorCard,
  type PriceForm,
} from "@/features/product-variants/PriceEditorCard";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { BadgeDollarSign, Layers, Plus, Tags } from "lucide-react";
import { useMemo, useState, useEffect, useRef } from "react";
import { useSearchParams } from "next/navigation";

type VariantForm = {
  productId: string;
  sku: string;
  barcode: string;
  color: string;
  size: string;
  season: string;
  unit: string;
  status: ProductStatus;
  costPrice: string;
  sellingPrice: string;
  sellingPriceBeforeVat: string;
  sellingPriceAfterVat: string;
  vatRate: string;
  costSource: string;
  trackLotExpiry: boolean;
  isBarcode: boolean;
};

const emptyForm = (productId = ""): VariantForm => ({
  productId,
  sku: "",
  barcode: "",
  color: "",
  size: "",
  season: "",
  unit: "",
  status: ProductStatus.Active,
  costPrice: "",
  sellingPrice: "",
  sellingPriceBeforeVat: "",
  sellingPriceAfterVat: "",
  vatRate: "",
  costSource: "",
  trackLotExpiry: false,
  isBarcode: false,
});

function toOptionalNumber(value: string): number | undefined {
  return parsePriceField(value);
}

function syncVariantFormPricing(form: VariantForm, priceForm: PriceForm): VariantForm {
  return {
    ...form,
    sellingPriceBeforeVat: priceForm.priceBeforeVat,
    sellingPriceAfterVat: priceForm.priceAfterVat,
    vatRate: priceForm.vatRate,
    sellingPrice: priceForm.priceAfterVat || form.sellingPrice,
  };
}

function priceFormFromVariant(v: ProductVariant): PriceForm {
  return {
    priceBeforeVat: v.sellingPriceBeforeVat != null ? String(v.sellingPriceBeforeVat) : "",
    vatRate: v.vatRate != null ? String(v.vatRate) : "",
    priceAfterVat: v.sellingPriceAfterVat != null ? String(v.sellingPriceAfterVat) : "",
    effectiveFrom: new Date().toISOString().slice(0, 10),
    effectiveTo: "",
  };
}

function priceFormFromProductPrice(price: ProductPrice): PriceForm {
  return {
    priceBeforeVat: String(price.priceBeforeVat),
    vatRate: String(price.vatRate),
    priceAfterVat: String(price.priceAfterVat),
    effectiveFrom: price.effectiveFrom.slice(0, 10),
    effectiveTo: price.effectiveTo?.slice(0, 10) ?? "",
  };
}

function calcMarginSnapshot(variant: ProductVariant) {
  if (
    variant.marginValueBeforeVat != null &&
    variant.marginRatePercent != null &&
    variant.sellingPriceBeforeVat != null &&
    variant.sellingPriceBeforeVat > 0
  ) {
    return {
      marginValue: variant.marginValueBeforeVat,
      marginRate: variant.marginRatePercent,
    };
  }

  const cost = variant.currentCostPrice ?? variant.costPrice;
  const sellingBeforeVat = variant.sellingPriceBeforeVat;
  if (cost == null || sellingBeforeVat == null || sellingBeforeVat <= 0) {
    return null;
  }

  const marginValue = sellingBeforeVat - cost;
  const marginRate = sellingBeforeVat === 0 ? 0 : (marginValue / sellingBeforeVat) * 100;
  return { marginValue, marginRate };
}

export default function ProductVariantsPage() {
  const t = useTranslations("variants");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const locale = useLocale();
  const qc = useQueryClient();
  const { notifyValidation, notifyError, confirm } = useNotify();
  const [page, setPage] = useState(1);
  const [brandId, setBrandId] = useState("");
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ProductVariant | null>(null);
  const [form, setForm] = useState<VariantForm>(emptyForm());
  const [regularPriceForm, setRegularPriceForm] = useState<PriceForm>(emptyPriceForm());
  const [promotionPriceForm, setPromotionPriceForm] = useState<PriceForm>(emptyPriceForm());
  const [markdownPriceForm, setMarkdownPriceForm] = useState<PriceForm>(emptyPriceForm());
  const searchParams = useSearchParams();
  const deepLinkHandled = useRef(false);

  const { data, isLoading } = useQuery({
    queryKey: ["product-variants", page, debouncedSearch, brandId],
    queryFn: () =>
      fetchProductVariants(page, 50, debouncedSearch || undefined, brandId || undefined),
  });

  const { data: brands } = useQuery({
    queryKey: ["brands-product-variants"],
    queryFn: fetchBrands,
    staleTime: 5 * 60_000,
  });

  const { data: products } = useQuery({
    queryKey: ["products-all"],
    queryFn: () => fetchProducts(1, 100),
  });

  const { data: priceHistory = [] } = useQuery({
    queryKey: ["product-price-history", editing?.id],
    queryFn: () => fetchProductPrices(editing!.id),
    enabled: modalOpen && !!editing?.id,
  });

  const hasFilters = hasSearch || brandId !== "";

  const clearFilters = () => {
    resetSearch();
    setBrandId("");
    setPage(1);
  };

  const handleBrandChange = (value: string) => {
    setBrandId(value);
    setPage(1);
  };

  const productMap = new Map(
    products?.items.map((p) => [p.id, p.name]) ?? []
  );

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    return {
      total: data?.totalCount ?? 0,
      active: items.filter((v) => isProductActive(v.status)).length,
      withCost: items.filter((v) => v.costPrice != null).length,
    };
  }, [data?.items, data?.totalCount]);

  const valuationPayload = () => {
    const costPrice = toOptionalNumber(form.costPrice);
    const sellingPriceBeforeVat =
      toOptionalNumber(regularPriceForm.priceBeforeVat) ??
      toOptionalNumber(form.sellingPriceBeforeVat);
    const vatRate =
      toOptionalNumber(regularPriceForm.vatRate) ?? toOptionalNumber(form.vatRate);
    const sellingPriceAfterVat =
      (sellingPriceBeforeVat != null && vatRate != null
        ? calcPriceAfterVat(sellingPriceBeforeVat, vatRate)
        : undefined) ??
      toOptionalNumber(regularPriceForm.priceAfterVat) ??
      toOptionalNumber(form.sellingPriceAfterVat) ??
      toOptionalNumber(form.sellingPrice);
    const sellingPrice = sellingPriceAfterVat;
    const costSource =
      costPrice !== undefined && form.costSource
        ? (Number(form.costSource) as CostSource)
        : undefined;

    return {
      costPrice,
      sellingPrice,
      sellingPriceBeforeVat,
      sellingPriceAfterVat,
      vatRate,
      costSource,
    };
  };

  const setRegularPriceFormSynced = (next: PriceForm) => {
    setRegularPriceForm(next);
    setForm((current) => syncVariantFormPricing(current, next));
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      const valuation = valuationPayload();
      if (editing) {
        return updateProductVariant(editing.id, {
          barcode: form.isBarcode ? undefined : form.barcode || undefined,
          color: form.color || undefined,
          size: form.size || undefined,
          season: form.season || undefined,
          unit: form.unit || undefined,
          status: form.status,
          trackLotExpiry: form.trackLotExpiry,
          isBarcode: form.isBarcode,
          ...valuation,
        });
      }
      return createProductVariant({
        productId: form.productId,
        sku: form.sku,
        barcode: form.isBarcode ? undefined : form.barcode || undefined,
        color: form.color || undefined,
        size: form.size || undefined,
        season: form.season || undefined,
        unit: form.unit || undefined,
        status: form.status,
        trackLotExpiry: form.trackLotExpiry,
        isBarcode: form.isBarcode,
        ...valuation,
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["product-variants"] });
      setModalOpen(false);
      setEditing(null);
    },
    onError: notifyError,
  });

  const deleteMutation = useMutation({
    mutationFn: deleteProductVariant,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["product-variants"] }),
    onError: notifyError,
  });

  const upsertPriceMutation = useMutation({
    mutationFn: async (input: {
      priceType: PriceType;
      form: PriceForm;
    }) => {
      if (!editing) {
        throw new Error("Price history can only be managed after the SKU is created.");
      }

      const before = parsePriceField(input.form.priceBeforeVat);
      const after = parsePriceField(input.form.priceAfterVat);
      const vatRate = Number(input.form.vatRate || 0);

      let priceBeforeVat = before ?? 0;
      let priceAfterVat = after ?? 0;
      if (before != null) {
        priceAfterVat = calcPriceAfterVat(before, vatRate);
      } else if (after != null) {
        priceBeforeVat = calcPriceBeforeVat(after, vatRate);
        priceAfterVat = after;
      }

      return upsertProductPrice(editing.id, {
        priceType: input.priceType,
        priceBeforeVat,
        vatRate,
        priceAfterVat,
        effectiveFrom: input.form.effectiveFrom,
        effectiveTo: input.form.effectiveTo || undefined,
      });
    },
    onSuccess: async (savedPrice, variables) => {
      const syncedPriceForm = priceFormFromProductPrice(savedPrice);
      if (variables.priceType === PriceType.Regular) {
        setRegularPriceFormSynced(syncedPriceForm);
      } else if (variables.priceType === PriceType.Promotion) {
        setPromotionPriceForm(syncedPriceForm);
      } else if (variables.priceType === PriceType.Markdown) {
        setMarkdownPriceForm(syncedPriceForm);
      }

      await qc.invalidateQueries({ queryKey: ["product-variants"] });
      await qc.invalidateQueries({ queryKey: ["product-price-history"] });
    },
    onError: notifyError,
  });

  function handleSave() {
    if (notifyValidation(validateProductVariantForm({
      productId: form.productId,
      sku: form.sku,
      costPrice: form.costPrice,
      sellingPriceBeforeVat: form.sellingPriceBeforeVat,
      sellingPriceAfterVat: form.sellingPriceAfterVat,
      vatRate: form.vatRate,
    }))) {
      return;
    }
    saveMutation.mutate();
  }

  function openCreate() {
    setEditing(null);
    setForm(emptyForm(products?.items[0]?.id ?? ""));
    setRegularPriceForm(emptyPriceForm());
    setPromotionPriceForm(emptyPriceForm());
    setMarkdownPriceForm(emptyPriceForm());
    setModalOpen(true);
  }

  function openEdit(v: ProductVariant) {
    setEditing(v);
    setForm({
      productId: v.productId,
      sku: v.sku,
      barcode: v.barcode ?? "",
      color: v.color ?? "",
      size: v.size ?? "",
      season: v.season ?? "",
      unit: v.unit ?? "",
      status: v.status,
      costPrice: v.costPrice != null ? String(v.costPrice) : "",
      sellingPrice:
        v.currentSellingPrice != null
          ? String(v.currentSellingPrice)
          : v.sellingPrice != null
            ? String(v.sellingPrice)
            : "",
      sellingPriceBeforeVat: v.sellingPriceBeforeVat != null ? String(v.sellingPriceBeforeVat) : "",
      sellingPriceAfterVat: v.sellingPriceAfterVat != null ? String(v.sellingPriceAfterVat) : "",
      vatRate: v.vatRate != null ? String(v.vatRate) : "",
      costSource: v.costSource != null ? String(v.costSource) : "",
      trackLotExpiry: v.trackLotExpiry ?? false,
      isBarcode: v.isBarcode ?? false,
    });
    const regularPrice = priceFormFromVariant(v);
    setRegularPriceForm(regularPrice);
    setForm((current) => syncVariantFormPricing(current, regularPrice));
    setPromotionPriceForm(emptyPriceForm());
    setMarkdownPriceForm(emptyPriceForm());
    setModalOpen(true);
  }

  useEffect(() => {
    const q = searchParams.get("search");
    if (q && search !== q) {
      setSearch(q);
    }
  }, [searchParams, search, setSearch]);

  useEffect(() => {
    if (deepLinkHandled.current || !data?.items.length) {
      return;
    }

    const variantId = searchParams.get("variantId");
    if (!variantId) {
      return;
    }

    const variant = data.items.find((item) => item.id === variantId);
    if (!variant) {
      return;
    }

    deepLinkHandled.current = true;
    openEdit(variant);

    const markdownBefore = searchParams.get("markdownBeforeVat");
    const markdownAfter = searchParams.get("markdownAfterVat");
    if (markdownBefore) {
      setMarkdownPriceForm({
        priceBeforeVat: markdownBefore,
        vatRate: variant.vatRate != null ? String(variant.vatRate) : "",
        priceAfterVat: markdownAfter ?? "",
        effectiveFrom: new Date().toISOString().slice(0, 10),
        effectiveTo: "",
      });
    }
  }, [data?.items, searchParams]);

  const hasCostPrice = form.costPrice.trim() !== "";
  const groupedPrices = useMemo(() => {
    return {
      regular: priceHistory.filter((p) => p.priceType === PriceType.Regular),
      promotion: priceHistory.filter((p) => p.priceType === PriceType.Promotion),
      markdown: priceHistory.filter((p) => p.priceType === PriceType.Markdown),
    };
  }, [priceHistory]);
  const currentPreviewCost = toOptionalNumber(form.costPrice);
  const currentPreviewBeforeVat = toOptionalNumber(form.sellingPriceBeforeVat);
  const currentPreviewAfterVat =
    toOptionalNumber(form.sellingPriceAfterVat) ?? toOptionalNumber(form.sellingPrice);
  const currentPreviewVat = toOptionalNumber(form.vatRate);
  const currentPreviewMargin =
    currentPreviewCost != null &&
    currentPreviewBeforeVat != null &&
    currentPreviewBeforeVat > 0
      ? {
          marginValue: currentPreviewBeforeVat - currentPreviewCost,
          marginRate:
            ((currentPreviewBeforeVat - currentPreviewCost) / currentPreviewBeforeVat) * 100,
        }
      : null;

  return (
    <div>
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <button className="btn-primary" onClick={openCreate}>
            <Plus className="h-4 w-4" />
            {t("create")}
          </button>
        }
      />

      {isLoading && !data ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <StatCard label={t("stats.total")} value={String(stats.total)} icon={Tags} accent="indigo" />
          <StatCard label={t("stats.active")} value={String(stats.active)} icon={Layers} accent="emerald" />
          <StatCard label={t("stats.withCost")} value={String(stats.withCost)} icon={BadgeDollarSign} accent="amber" />
        </div>
      )}

      <ListFilterBar
        variant="enhanced"
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchSku")}
        onReset={clearFilters}
        showReset={hasFilters}
      >
        <label className="min-w-[200px] text-sm text-slate-600">
          <span className="mb-1 block text-xs font-medium uppercase tracking-wide text-slate-500">
            {t("brand")}
          </span>
          <select className="input" value={brandId} onChange={(e) => handleBrandChange(e.target.value)}>
            <option value="">{t("allBrands")}</option>
            {brands?.map((brand) => (
              <option key={brand.id} value={brand.id}>
                {brand.name}
              </option>
            ))}
          </select>
        </label>
      </ListFilterBar>

      <div className="card mb-6 overflow-hidden">
        <div className="border-b border-slate-100 bg-gradient-to-r from-slate-50/80 to-white px-5 py-4">
          <h3 className="text-sm font-semibold text-slate-900">{t("pricingDomainTitle")}</h3>
          <p className="mt-1 text-sm text-slate-500">{t("pricingDomainSubtitle")}</p>
        </div>
        <div className="grid gap-3 p-5 md:grid-cols-2 xl:grid-cols-3">
          {[
            { title: t("regularPriceTitle"), desc: t("regularPriceDesc"), status: t("availableNow") },
            { title: t("promotionPriceTitle"), desc: t("promotionPriceDesc"), status: t("comingSoon") },
            { title: t("markdownTitle"), desc: t("markdownDesc"), status: t("comingSoon") },
            { title: t("currentCostTitle"), desc: t("currentCostDesc"), status: t("availableNow") },
            { title: t("inventoryValuationTitle"), desc: t("inventoryValuationDesc"), status: t("availableNow") },
            { title: t("marginSnapshotTitle"), desc: t("marginSnapshotDesc"), status: t("availableNow") },
          ].map((item) => (
            <div
              key={item.title}
              className="rounded-2xl border border-slate-100 bg-slate-50/70 p-4"
            >
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-slate-900">{item.title}</p>
                  <p className="mt-1 text-xs leading-5 text-slate-500">{item.desc}</p>
                </div>
                <span className="rounded-full bg-white px-2.5 py-1 text-[11px] font-medium text-slate-500 ring-1 ring-slate-200">
                  {item.status}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>

      <DataTableCard
        title={t("title")}
        icon={Tags}
        count={data?.totalCount}
        countLabel={tCommon("total")}
      >
        {isLoading ? (
          <TableSkeleton rows={8} cols={8} />
        ) : !data?.items.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <>
            <div className="table-wrap max-h-[32rem] overflow-y-auto scrollbar-thin">
              <table className="data-table">
                <thead className="sticky top-0 z-10 bg-white">
                  <tr>
                    <th>{t("sku")}</th>
                    <th>{t("product")}</th>
                    <th>{t("currentCostTitle")}</th>
                    <th>{t("sellingPriceBeforeVat")}</th>
                    <th>{t("vatRate")}</th>
                    <th>{t("sellingPriceAfterVat")}</th>
                    <th>{t("marginSnapshotTitle")}</th>
                    <th>{t("costSource")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((v) => (
                    <tr key={v.id} className="hover:bg-slate-50/80">
                      <td><CodePill>{v.sku}</CodePill></td>
                      <td className="font-medium text-slate-900">
                        {productMap.get(v.productId) ?? v.productId}
                      </td>
                      <td className="tabular-nums">
                        {v.currentCostPrice != null || v.costPrice != null
                          ? formatNumber(v.currentCostPrice ?? v.costPrice ?? 0, locale)
                          : "—"}
                      </td>
                      <td className="tabular-nums">
                        {v.sellingPriceBeforeVat != null
                          ? formatNumber(v.sellingPriceBeforeVat, locale)
                          : "—"}
                      </td>
                      <td className="tabular-nums">
                        {v.vatRate != null ? `${formatNumber(v.vatRate, locale)}%` : "—"}
                      </td>
                      <td className="tabular-nums font-medium text-slate-900">
                        {v.sellingPriceAfterVat != null || v.currentSellingPrice != null || v.sellingPrice != null
                          ? formatNumber(v.sellingPriceAfterVat ?? v.currentSellingPrice ?? v.sellingPrice ?? 0, locale)
                          : "—"}
                      </td>
                      <td className="text-xs">
                        {(() => {
                          const margin = calcMarginSnapshot(v);
                          if (!margin) return "—";
                          const tone =
                            margin.marginValue >= 0 ? "text-emerald-700" : "text-rose-700";
                          return (
                            <div className={tone}>
                              <div className="font-medium">
                                {formatNumber(margin.marginValue, locale)}
                              </div>
                              <div>{formatNumber(margin.marginRate, locale)}%</div>
                            </div>
                          );
                        })()}
                      </td>
                      <td className="text-xs">
                        {(v.currentCostSource ?? v.costSource) != null
                          ? t(`costSources.${costSourceKey((v.currentCostSource ?? v.costSource) as CostSource)}` as "costSources.Manual")
                          : "—"}
                      </td>
                      <td>
                        <ActiveBadge
                          active={isProductActive(v.status)}
                          label={
                            isProductActive(v.status)
                              ? tCommon("active")
                              : tCommon("inactive")
                          }
                        />
                      </td>
                      <td className="space-x-2 whitespace-nowrap">
                        <button
                          className="text-sm font-medium text-brand-600 hover:text-brand-700"
                          onClick={() => openEdit(v)}
                        >
                          {tCommon("edit")}
                        </button>
                        <button
                          className="text-sm font-medium text-red-600 hover:text-red-700"
                          onClick={async () => {
                            if (await confirm(t("deleteConfirm"))) {
                              deleteMutation.mutate(v.id);
                            }
                          }}
                        >
                          {tCommon("delete")}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </DataTableCard>

      <FormModal
        open={modalOpen}
        title={editing ? tCommon("edit") : t("create")}
        onClose={() => setModalOpen(false)}
        size="lg"
        footer={
          <>
            <button className="btn-secondary" onClick={() => setModalOpen(false)}>
              {tCommon("cancel")}
            </button>
            <button
              className="btn-primary"
              disabled={saveMutation.isPending}
              onClick={handleSave}
            >
              {tCommon("save")}
            </button>
          </>
        }
      >
        <div className="space-y-3">
              {!editing && (
                <>
                  <div>
                    <label className="mb-1 block text-sm">{t("product")} *</label>
                    <select
                      className="input"
                      value={form.productId}
                      onChange={(e) =>
                        setForm({ ...form, productId: e.target.value })
                      }
                    >
                      {products?.items.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.productCode} — {p.name}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="mb-1 block text-sm">{t("sku")} *</label>
                    <input
                      className="input"
                      value={form.sku}
                      onChange={(e) => setForm({ ...form, sku: e.target.value })}
                    />
                  </div>
                </>
              )}
              {["color", "size", "season", "unit"].map((field) => (
                <div key={field}>
                  <label className="mb-1 block text-sm">
                    {t(field as "barcode")}
                  </label>
                  <input
                    className="input"
                    value={form[field as keyof VariantForm] as string}
                    onChange={(e) =>
                      setForm({ ...form, [field]: e.target.value })
                    }
                  />
                </div>
              ))}

              <div className="flex flex-wrap gap-4 border-t border-slate-100 pt-3">
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={form.trackLotExpiry}
                    onChange={(e) =>
                      setForm({ ...form, trackLotExpiry: e.target.checked })
                    }
                  />
                  {t("trackLotExpiry")}
                </label>
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={form.isBarcode}
                    onChange={(e) =>
                      setForm({
                        ...form,
                        isBarcode: e.target.checked,
                        barcode: e.target.checked ? "" : form.barcode,
                      })
                    }
                  />
                  {t("isBarcode")}
                </label>
              </div>

              <div className="space-y-4 border-t border-slate-100 pt-4">
                <PriceEditorCard
                  title={t("regularPriceTitle")}
                  description={t("regularPriceDesc")}
                  formState={regularPriceForm}
                  setFormState={setRegularPriceFormSynced}
                  priceType={PriceType.Regular}
                  history={groupedPrices.regular}
                  canSavePrice={!!editing}
                  savePending={upsertPriceMutation.isPending}
                  onSave={(priceType, form) => upsertPriceMutation.mutate({ priceType, form })}
                />

                <PriceEditorCard
                  title={t("promotionPriceTitle")}
                  description={t("promotionPriceDesc")}
                  badge={t("availableNow")}
                  formState={promotionPriceForm}
                  setFormState={setPromotionPriceForm}
                  priceType={PriceType.Promotion}
                  history={groupedPrices.promotion}
                  canSavePrice={!!editing}
                  savePending={upsertPriceMutation.isPending}
                  onSave={(priceType, form) => upsertPriceMutation.mutate({ priceType, form })}
                />

                <PriceEditorCard
                  title={t("markdownTitle")}
                  description={t("markdownDesc")}
                  badge={t("availableNow")}
                  formState={markdownPriceForm}
                  setFormState={setMarkdownPriceForm}
                  priceType={PriceType.Markdown}
                  history={groupedPrices.markdown}
                  canSavePrice={!!editing}
                  savePending={upsertPriceMutation.isPending}
                  onSave={(priceType, form) => upsertPriceMutation.mutate({ priceType, form })}
                />

                <div className="rounded-2xl border border-slate-100 bg-slate-50/60 p-4">
                  <div className="mb-3">
                    <p className="text-sm font-semibold text-slate-900">{t("currentCostTitle")}</p>
                    <p className="mt-1 text-xs text-slate-500">{t("currentCostDesc")}</p>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="mb-1 block text-sm">{t("costPrice")}</label>
                    <input
                      type="number"
                      min={0}
                      step="any"
                      className="input"
                      placeholder="—"
                      value={form.costPrice}
                      onChange={(e) =>
                        setForm({
                          ...form,
                          costPrice: e.target.value,
                          costSource:
                            e.target.value.trim() === "" ? "" : form.costSource,
                        })
                      }
                    />
                  </div>
                  {hasCostPrice && (
                  <div className="mt-3">
                    <label className="mb-1 block text-sm">{t("costSource")}</label>
                    <select
                      className="input"
                      value={form.costSource || String(CostSource.Manual)}
                      onChange={(e) =>
                        setForm({ ...form, costSource: e.target.value })
                      }
                    >
                      {Object.values(CostSource)
                        .filter((v) => typeof v === "number")
                        .map((s) => (
                          <option key={s} value={s}>
                            {t(
                              `costSources.${costSourceKey(s as CostSource)}` as "costSources.Manual"
                            )}
                          </option>
                        ))}
                    </select>
                  </div>
                  )}
                  </div>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="rounded-2xl border border-slate-100 bg-slate-50/60 p-4">
                    <p className="text-sm font-semibold text-slate-900">{t("inventoryValuationTitle")}</p>
                    <p className="mt-1 text-xs text-slate-500">{t("inventoryValuationDesc")}</p>
                    <div className="mt-3 rounded-xl bg-white px-3 py-2 text-sm text-slate-700 ring-1 ring-slate-100">
                      {currentPreviewCost != null
                        ? `${t("availableNow")}: ${t("currentCostTitle")} = ${formatNumber(currentPreviewCost, locale)}`
                        : t("inventoryValuationHint")}
                    </div>
                  </div>

                  <div className="rounded-2xl border border-slate-100 bg-slate-50/60 p-4">
                    <p className="text-sm font-semibold text-slate-900">{t("marginSnapshotTitle")}</p>
                    <p className="mt-1 text-xs text-slate-500">{t("marginSnapshotDesc")}</p>
                    <div className="mt-3 rounded-xl bg-white px-3 py-2 text-sm text-slate-700 ring-1 ring-slate-100">
                      {currentPreviewMargin ? (
                        <div className="space-y-1">
                          <div>
                            {t("marginValueLabel")}: {formatNumber(currentPreviewMargin.marginValue, locale)}
                          </div>
                          <div>
                            {t("marginRateLabel")}: {formatNumber(currentPreviewMargin.marginRate, locale)}%
                          </div>
                          {currentPreviewAfterVat != null && currentPreviewVat != null && (
                            <div className="text-xs text-slate-500">
                              {t("sellingPriceAfterVat")}: {formatNumber(currentPreviewAfterVat, locale)} | {t("vatRate")}: {formatNumber(currentPreviewVat, locale)}%
                            </div>
                          )}
                        </div>
                      ) : (
                        t("marginSnapshotHint")
                      )}
                    </div>
                  </div>
                </div>
              </div>

              <div>
                <label className="mb-1 block text-sm">{tCommon("status")}</label>
                <select
                  className="input"
                  value={form.status}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      status: Number(e.target.value) as ProductStatus,
                    })
                  }
                >
                  <option value={ProductStatus.Active}>
                    {tCommon("active")}
                  </option>
                  <option value={ProductStatus.Inactive}>
                    {tCommon("inactive")}
                  </option>
                </select>
              </div>
            </div>
      </FormModal>
    </div>
  );
}
