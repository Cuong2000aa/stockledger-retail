"use client";

import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { ActiveBadge, costSourceKey, isProductActive } from "@/components/StatusBadge";
import {
  createProductVariant,
  deleteProductVariant,
  fetchProductVariants,
  fetchProducts,
  getApiErrorMessage,
  updateProductVariant,
} from "@/lib/api";
import { formatNumber } from "@/lib/format";
import { CostSource, ProductStatus, type ProductVariant } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

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
});

function toOptionalNumber(value: string): number | undefined {
  if (value.trim() === "") return undefined;
  const n = Number(value);
  return Number.isNaN(n) ? undefined : n;
}

export default function ProductVariantsPage() {
  const t = useTranslations("variants");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ProductVariant | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<VariantForm>(emptyForm());

  const { data, isLoading } = useQuery({
    queryKey: ["product-variants", page],
    queryFn: () => fetchProductVariants(page),
  });

  const { data: products } = useQuery({
    queryKey: ["products-all"],
    queryFn: () => fetchProducts(1, 100),
  });

  const productMap = new Map(
    products?.items.map((p) => [p.id, p.name]) ?? []
  );

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
          barcode: form.barcode || undefined,
          color: form.color || undefined,
          size: form.size || undefined,
          season: form.season || undefined,
          unit: form.unit || undefined,
          status: form.status,
          ...valuation,
        });
      }
      return createProductVariant({
        productId: form.productId,
        sku: form.sku,
        barcode: form.barcode || undefined,
        color: form.color || undefined,
        size: form.size || undefined,
        season: form.season || undefined,
        unit: form.unit || undefined,
        status: form.status,
        ...valuation,
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["product-variants"] });
      setModalOpen(false);
      setError(null);
    },
    onError: (e) => setError(getApiErrorMessage(e)),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteProductVariant,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["product-variants"] }),
    onError: (e) => alert(getApiErrorMessage(e)),
  });

  function openCreate() {
    setEditing(null);
    setForm(emptyForm(products?.items[0]?.id ?? ""));
    setError(null);
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
    });
    setError(null);
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
            + {t("create")}
          </button>
        }
      />

      <div className="card">
        {isLoading ? (
          <p className="p-6 text-slate-500">{tCommon("loading")}</p>
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>{t("sku")}</th>
                    <th>{t("product")}</th>
                    <th>{t("costPrice")}</th>
                    <th>{t("sellingPrice")}</th>
                    <th>{t("costSource")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.map((v) => (
                    <tr key={v.id}>
                      <td className="font-mono text-xs">{v.sku}</td>
                      <td>{productMap.get(v.productId) ?? v.productId}</td>
                      <td>
                        {v.costPrice != null
                          ? formatNumber(v.costPrice, locale)
                          : "—"}
                      </td>
                      <td>
                        {v.sellingPrice != null
                          ? formatNumber(v.sellingPrice, locale)
                          : "—"}
                      </td>
                      <td className="text-xs">
                        {v.costSource != null
                          ? t(
                              `costSources.${costSourceKey(v.costSource)}` as "costSources.Manual"
                            )
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
                      <td className="space-x-2">
                        <button
                          className="text-brand-600 hover:underline"
                          onClick={() => openEdit(v)}
                        >
                          {tCommon("edit")}
                        </button>
                        <button
                          className="text-red-600 hover:underline"
                          onClick={() => {
                            if (confirm(t("deleteConfirm")))
                              deleteMutation.mutate(v.id);
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
            {data && (
              <Pagination
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                onChange={setPage}
              />
            )}
          </>
        )}
      </div>

      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="card max-h-[90vh] w-full max-w-md overflow-y-auto p-6">
            <h2 className="mb-4 text-lg font-semibold">
              {editing ? tCommon("edit") : t("create")}
            </h2>
            {error && (
              <p className="mb-3 rounded-lg bg-red-50 p-2 text-sm text-red-700">
                {error}
              </p>
            )}
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
              {["barcode", "color", "size", "season", "unit"].map((field) => (
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
            <div className="mt-6 flex justify-end gap-2">
              <button className="btn-secondary" onClick={() => setModalOpen(false)}>
                {tCommon("cancel")}
              </button>
              <button
                className="btn-primary"
                disabled={saveMutation.isPending}
                onClick={() => saveMutation.mutate()}
              >
                {tCommon("save")}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
