"use client";

import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { FormModal } from "@/components/FormModal";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton, StatCardsSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
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
import { Layers, Package, Plus, Tags } from "lucide-react";
import { useMemo, useState } from "react";

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

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    const active = items.filter((p) => isProductActive(p.status)).length;
    const brands = new Set(items.map((p) => p.brand).filter(Boolean)).size;
    return {
      total: data?.totalCount ?? 0,
      active,
      inactive: items.length - active,
      brands,
    };
  }, [data?.items, data?.totalCount]);

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

  const modalFooter = (
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
  );

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
        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard label={t("stats.total")} value={String(stats.total)} icon={Package} accent="indigo" />
          <StatCard label={t("stats.active")} value={String(stats.active)} icon={Layers} accent="emerald" />
          <StatCard label={t("stats.inactive")} value={String(stats.inactive)} icon={Package} accent="amber" />
          <StatCard label={t("stats.brands")} value={String(stats.brands)} icon={Tags} accent="sky" />
        </div>
      )}

      <ListFilterBar
        variant="enhanced"
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchProduct")}
        onReset={resetSearch}
        showReset={hasSearch}
      />

      <DataTableCard
        title={t("title")}
        icon={Package}
        count={data?.totalCount}
        countLabel={tCommon("total")}
      >
        {isLoading ? (
          <TableSkeleton rows={8} cols={6} />
        ) : !data?.items.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <>
            <div className="table-wrap max-h-[32rem] overflow-y-auto scrollbar-thin">
              <table className="data-table">
                <thead className="sticky top-0 z-10 bg-white">
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
                  {data.items.map((p) => (
                    <tr key={p.id} className="hover:bg-slate-50/80">
                      <td><CodePill>{p.productCode}</CodePill></td>
                      <td className="font-medium text-slate-900">{p.name}</td>
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
                      <td className="space-x-2 whitespace-nowrap">
                        <button
                          className="text-sm font-medium text-brand-600 hover:text-brand-700"
                          onClick={() => openEdit(p)}
                        >
                          {tCommon("edit")}
                        </button>
                        <button
                          className="text-sm font-medium text-red-600 hover:text-red-700"
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
        footer={modalFooter}
      >
        <div className="space-y-3">
          {!editing && (
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">{t("code")} *</label>
              <input
                className="input"
                value={form.productCode}
                onChange={(e) => setForm({ ...form, productCode: e.target.value })}
              />
            </div>
          )}
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">{t("name")} *</label>
            <input
              className="input"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">{t("brand")}</label>
              <input
                className="input"
                value={form.brand}
                onChange={(e) => setForm({ ...form, brand: e.target.value })}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">{t("category")}</label>
              <input
                className="input"
                value={form.category}
                onChange={(e) => setForm({ ...form, category: e.target.value })}
              />
            </div>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">{tCommon("status")}</label>
            <select
              className="input"
              value={form.status}
              onChange={(e) =>
                setForm({ ...form, status: Number(e.target.value) as ProductStatus })
              }
            >
              <option value={ProductStatus.Active}>{tCommon("active")}</option>
              <option value={ProductStatus.Inactive}>{tCommon("inactive")}</option>
            </select>
          </div>
        </div>
      </FormModal>
    </div>
  );
}
