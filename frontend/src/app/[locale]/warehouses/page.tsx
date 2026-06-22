"use client";

import { ListFilterBar } from "@/components/ListFilterBar";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { warehouseTypeKey } from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { useNotify } from "@/hooks/useNotify";
import {
  createWarehouse,
  deleteWarehouse,
  fetchWarehouses,
  updateWarehouse,
} from "@/lib/api";
import { validateWarehouseForm } from "@/lib/validation";
import {
  WarehouseStatus,
  WarehouseType,
  type Warehouse,
} from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useState } from "react";

export default function WarehousesPage() {
  const t = useTranslations("warehouses");
  const tTypes = useTranslations("warehouseTypes");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const qc = useQueryClient();
  const { notifyValidation, notifyError, confirm } = useNotify();
  const [page, setPage] = useState(1);
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Warehouse | null>(null);

  const [form, setForm] = useState({
    code: "",
    name: "",
    type: WarehouseType.Store,
    parentWarehouseId: "",
    status: WarehouseStatus.Active,
  });

  const { data, isLoading } = useQuery({
    queryKey: ["warehouses", page, debouncedSearch],
    queryFn: () => fetchWarehouses(page, 20, debouncedSearch || undefined),
  });

  const saveMutation = useMutation({
    mutationFn: async () => {
      const parentId = form.parentWarehouseId || undefined;
      if (editing) {
        return updateWarehouse(editing.id, {
          name: form.name,
          type: form.type,
          parentWarehouseId: parentId,
          status: form.status,
        });
      }
      return createWarehouse({
        code: form.code,
        name: form.name,
        type: form.type,
        parentWarehouseId: parentId,
        status: form.status,
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["warehouses"] });
      setModalOpen(false);
    },
    onError: notifyError,
  });

  const deleteMutation = useMutation({
    mutationFn: deleteWarehouse,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["warehouses"] }),
    onError: notifyError,
  });

  function handleSave() {
    if (notifyValidation(validateWarehouseForm(form, !!editing))) {
      return;
    }
    saveMutation.mutate();
  }

  function openCreate() {
    setEditing(null);
    setForm({
      code: "",
      name: "",
      type: WarehouseType.Store,
      parentWarehouseId: "",
      status: WarehouseStatus.Active,
    });
    setModalOpen(true);
  }

  function openEdit(w: Warehouse) {
    setEditing(w);
    setForm({
      code: w.code,
      name: w.name,
      type: w.type,
      parentWarehouseId: w.parentWarehouseId ?? "",
      status: w.status,
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
        searchPlaceholder={tFilters("searchWarehouse")}
        onReset={resetSearch}
        showReset={hasSearch}
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
                    <th>{t("code")}</th>
                    <th>{t("name")}</th>
                    <th>{t("type")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.map((w) => (
                    <tr key={w.id}>
                      <td className="font-mono text-xs">{w.code}</td>
                      <td>{w.name}</td>
                      <td>{tTypes(warehouseTypeKey(w.type))}</td>
                      <td>
                        {w.status === WarehouseStatus.Active
                          ? tCommon("active")
                          : tCommon("inactive")}
                      </td>
                      <td className="space-x-2">
                        <button
                          className="text-brand-600 hover:underline"
                          onClick={() => openEdit(w)}
                        >
                          {tCommon("edit")}
                        </button>
                        <button
                          className="text-red-600 hover:underline"
                          onClick={async () => {
                            if (await confirm(t("deleteConfirm"))) {
                              deleteMutation.mutate(w.id);
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
                    value={form.code}
                    onChange={(e) => setForm({ ...form, code: e.target.value })}
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
                <label className="mb-1 block text-sm">{t("type")}</label>
                <select
                  className="input"
                  value={form.type}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      type: Number(e.target.value) as WarehouseType,
                    })
                  }
                >
                  {Object.values(WarehouseType)
                    .filter((v) => typeof v === "number")
                    .map((v) => (
                      <option key={v} value={v}>
                        {tTypes(warehouseTypeKey(v as WarehouseType))}
                      </option>
                    ))}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-sm">{t("parent")}</label>
                <select
                  className="input"
                  value={form.parentWarehouseId}
                  onChange={(e) =>
                    setForm({ ...form, parentWarehouseId: e.target.value })
                  }
                >
                  <option value="">—</option>
                  {data?.items
                    .filter((w) => w.id !== editing?.id)
                    .map((w) => (
                      <option key={w.id} value={w.id}>
                        {w.code} — {w.name}
                      </option>
                    ))}
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
