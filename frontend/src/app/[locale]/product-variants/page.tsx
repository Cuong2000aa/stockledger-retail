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
  fetchProductVariants,
  fetchProducts,
  updateProductVariant,
} from "@/lib/api";
import { validateProductVariantForm } from "@/lib/validation";
import { formatNumber } from "@/lib/format";
import { CostSource, ProductStatus, type ProductVariant } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { BadgeDollarSign, Layers, Plus, Tags } from "lucide-react";
import { useMemo, useState } from "react";

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
  costSource: "",
  trackLotExpiry: false,
  isBarcode: false,
});

function toOptionalNumber(value: string): number | undefined {
  if (value.trim() === "") return undefined;
  const n = Number(value);
  return Number.isNaN(n) ? undefined : n;
}

export default function ProductVariantsPage() {
  const t = useTranslations("variants");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const locale = useLocale();
  const qc = useQueryClient();
  const { notifyValidation, notifyError, confirm } = useNotify();
  const [page, setPage] = useState(1);
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ProductVariant | null>(null);
  const [form, setForm] = useState<VariantForm>(emptyForm());

  const { data, isLoading } = useQuery({
    queryKey: ["product-variants", page, debouncedSearch],
    queryFn: () => fetchProductVariants(page, 50, debouncedSearch || undefined),
  });

  const { data: products } = useQuery({
    queryKey: ["products-all"],
    queryFn: () => fetchProducts(1, 100),
  });

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
    const sellingPrice = toOptionalNumber(form.sellingPrice);
    const costSource =
      costPrice !== undefined && form.costSource
        ? (Number(form.costSource) as CostSource)
        : undefined;

    return { costPrice, sellingPrice, costSource };
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

  function handleSave() {
    if (notifyValidation(validateProductVariantForm(form))) {
      return;
    }
    saveMutation.mutate();
  }

  function openCreate() {
    setEditing(null);
    setForm(emptyForm(products?.items[0]?.id ?? ""));
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
      sellingPrice: v.sellingPrice != null ? String(v.sellingPrice) : "",
      costSource: v.costSource != null ? String(v.costSource) : "",
      trackLotExpiry: v.trackLotExpiry ?? false,
      isBarcode: v.isBarcode ?? false,
    });
    setModalOpen(true);
  }

  const hasCostPrice = form.costPrice.trim() !== "";

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
        onReset={resetSearch}
        showReset={hasSearch}
      />

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
                    <th>{t("barcode")}</th>
                    <th>{t("costPrice")}</th>
                    <th>{t("sellingPrice")}</th>
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
                      <td className="text-sm">
                        {v.isBarcode ? (
                          <span className="font-medium text-emerald-700">
                            {tCommon("yes")}
                          </span>
                        ) : (
                          <span className="text-slate-400">{tCommon("no")}</span>
                        )}
                      </td>
                      <td className="tabular-nums">
                        {v.costPrice != null ? formatNumber(v.costPrice, locale) : "—"}
                      </td>
                      <td className="tabular-nums font-medium text-slate-900">
                        {v.sellingPrice != null ? formatNumber(v.sellingPrice, locale) : "—"}
                      </td>
                      <td className="text-xs">
                        {v.costSource != null
                          ? t(`costSources.${costSourceKey(v.costSource)}` as "costSources.Manual")
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

              <div className="border-t border-slate-100 pt-3">
                <p className="mb-2 text-xs font-semibold uppercase text-slate-500">
                  {t("costPrice")} / {t("sellingPrice")}
                </p>
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
                  <div>
                    <label className="mb-1 block text-sm">{t("sellingPrice")}</label>
                    <input
                      type="number"
                      min={0}
                      step="any"
                      className="input"
                      placeholder="—"
                      value={form.sellingPrice}
                      onChange={(e) =>
                        setForm({ ...form, sellingPrice: e.target.value })
                      }
                    />
                  </div>
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
