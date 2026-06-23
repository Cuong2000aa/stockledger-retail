"use client";

import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { ActiveBadge, isProductActive } from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { useNotify } from "@/hooks/useNotify";
import {
  createProduct,
  deleteProduct,
  fetchProducts,
  updateProduct,
} from "@/lib/api";
import { validateProductForm } from "@/lib/validation";
import { ProductStatus, type Product } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useState } from "react";

export default function ProductsPage() {
  const t = useTranslations("products");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const qc = useQueryClient();
  const { notifyValidation, notifyError, confirm } = useNotify();
  const [page, setPage] = useState(1);
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);

  const [form, setForm] = useState({
    productCode: "",
    name: "",
    brand: "",
    category: "",
    status: ProductStatus.Active,
  });

  const { data, isLoading } = useQuery({
    queryKey: ["products", page, debouncedSearch],
    queryFn: () => fetchProducts(page, 20, debouncedSearch || undefined),
  });

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (editing) {
        return updateProduct(editing.id, {
          name: form.name,
          brand: form.brand || undefined,
          category: form.category || undefined,
          status: form.status,
        });
      }
      return createProduct({
        productCode: form.productCode,
        name: form.name,
        brand: form.brand || undefined,
        category: form.category || undefined,
        status: form.status,
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["products"] });
      setModalOpen(false);
      setEditing(null);
    },
    onError: notifyError,
  });

  const deleteMutation = useMutation({
    mutationFn: deleteProduct,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["products"] }),
    onError: notifyError,
  });

  function handleSave() {
    if (notifyValidation(validateProductForm(form, !!editing))) {
      return;
    }
    saveMutation.mutate();
  }

  function openCreate() {
    setEditing(null);
    setForm({
      productCode: "",
      name: "",
      brand: "",
      category: "",
      status: ProductStatus.Active,
    });
    setModalOpen(true);
  }

  function openEdit(p: Product) {
    setEditing(p);
    setForm({
      productCode: p.productCode,
      name: p.name,
      brand: p.brand ?? "",
      category: p.category ?? "",
      status: p.status,
    });
    setModalOpen(true);
  }

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

      <ListFilterBar
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchProduct")}
        onReset={resetSearch}
        showReset={hasSearch}
      />

      <div className="card">
        {isLoading ? (
          <TableSkeleton rows={8} cols={5} />
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>{t("code")}</th>
                    <th>{t("name")}</th>
                    <th>{t("brand")}</th>
                    <th>{t("category")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.length === 0 && (
                    <tr>
                      <td colSpan={6} className="text-center text-slate-400">
                        {tCommon("noData")}
                      </td>
                    </tr>
                  )}
                  {data?.items.map((p) => (
                    <tr key={p.id}>
                      <td className="font-mono text-xs">{p.productCode}</td>
                      <td>{p.name}</td>
                      <td>{p.brand ?? "—"}</td>
                      <td>{p.category ?? "—"}</td>
                      <td>
                        <ActiveBadge
                          active={isProductActive(p.status)}
                          label={
                            isProductActive(p.status)
                              ? tCommon("active")
                              : tCommon("inactive")
                          }
                        />
                      </td>
                      <td className="space-x-2">
                        <button
                          className="text-brand-600 hover:underline"
                          onClick={() => openEdit(p)}
                        >
                          {tCommon("edit")}
                        </button>
                        <button
                          className="text-red-600 hover:underline"
                          onClick={async () => {
                            if (await confirm(t("deleteConfirm"))) {
                              deleteMutation.mutate(p.id);
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
          <div className="card w-full max-w-md p-6">
            <h2 className="mb-4 text-lg font-semibold">
              {editing ? tCommon("edit") : t("create")}
            </h2>
            <div className="space-y-3">
              {!editing && (
                <div>
                  <label className="mb-1 block text-sm">{t("code")} *</label>
                  <input
                    className="input"
                    value={form.productCode}
                    onChange={(e) =>
                      setForm({ ...form, productCode: e.target.value })
                    }
                  />
                </div>
              )}
              <div>
                <label className="mb-1 block text-sm">{t("name")} *</label>
                <input
                  className="input"
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </div>
              <div>
                <label className="mb-1 block text-sm">{t("brand")}</label>
                <input
                  className="input"
                  value={form.brand}
                  onChange={(e) => setForm({ ...form, brand: e.target.value })}
                />
              </div>
              <div>
                <label className="mb-1 block text-sm">{t("category")}</label>
                <input
                  className="input"
                  value={form.category}
                  onChange={(e) =>
                    setForm({ ...form, category: e.target.value })
                  }
                />
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
                onClick={handleSave}
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
