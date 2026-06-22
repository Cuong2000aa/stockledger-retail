"use client";

import { ListFilterBar } from "@/components/ListFilterBar";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { ActiveBadge } from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { useNotify } from "@/hooks/useNotify";
import {
  createSupplier,
  deleteSupplier,
  fetchSuppliers,
  updateSupplier,
} from "@/lib/api";
import { validateSupplierForm } from "@/lib/validation";
import { SupplierStatus, type Supplier } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useState } from "react";

export default function SuppliersPage() {
  const t = useTranslations("suppliers");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const qc = useQueryClient();
  const { notifyValidation, notifyError, confirm } = useNotify();
  const [page, setPage] = useState(1);
  const { search, setSearch, debouncedSearch, resetSearch, hasSearch } =
    useListSearch(() => setPage(1));
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Supplier | null>(null);

  const [form, setForm] = useState({
    code: "",
    name: "",
    contactName: "",
    phone: "",
    email: "",
    address: "",
    status: SupplierStatus.Active,
  });

  const { data, isLoading } = useQuery({
    queryKey: ["suppliers", page, debouncedSearch],
    queryFn: () => fetchSuppliers(page, 20, debouncedSearch || undefined),
  });

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (editing) {
        return updateSupplier(editing.id, {
          name: form.name,
          contactName: form.contactName || undefined,
          phone: form.phone || undefined,
          email: form.email || undefined,
          address: form.address || undefined,
          status: form.status,
        });
      }
      return createSupplier({
        code: form.code,
        name: form.name,
        contactName: form.contactName || undefined,
        phone: form.phone || undefined,
        email: form.email || undefined,
        address: form.address || undefined,
        status: form.status,
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["suppliers"] });
      setModalOpen(false);
    },
    onError: notifyError,
  });

  const deleteMutation = useMutation({
    mutationFn: deleteSupplier,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["suppliers"] }),
    onError: notifyError,
  });

  function handleSave() {
    if (notifyValidation(validateSupplierForm(form, !!editing))) {
      return;
    }
    saveMutation.mutate();
  }

  function openCreate() {
    setEditing(null);
    setForm({
      code: "",
      name: "",
      contactName: "",
      phone: "",
      email: "",
      address: "",
      status: SupplierStatus.Active,
    });
    setModalOpen(true);
  }

  function openEdit(s: Supplier) {
    setEditing(s);
    setForm({
      code: s.code,
      name: s.name,
      contactName: s.contactName ?? "",
      phone: s.phone ?? "",
      email: s.email ?? "",
      address: s.address ?? "",
      status: s.status,
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
        searchPlaceholder={tFilters("searchSupplier")}
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
                    <th>{t("contact")}</th>
                    <th>{t("phone")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.map((s) => (
                    <tr key={s.id}>
                      <td className="font-mono text-xs">{s.code}</td>
                      <td>{s.name}</td>
                      <td>{s.contactName ?? "—"}</td>
                      <td>{s.phone ?? "—"}</td>
                      <td>
                        <ActiveBadge
                          active={s.status === SupplierStatus.Active}
                          label={
                            s.status === SupplierStatus.Active
                              ? tCommon("active")
                              : tCommon("inactive")
                          }
                        />
                      </td>
                      <td className="space-x-2">
                        <button
                          className="text-brand-600 hover:underline"
                          onClick={() => openEdit(s)}
                        >
                          {tCommon("edit")}
                        </button>
                        <button
                          className="text-red-600 hover:underline"
                          onClick={async () => {
                            if (await confirm(t("deleteConfirm"))) {
                              deleteMutation.mutate(s.id);
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
          <div className="card w-full max-w-lg p-6">
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
                <label className="mb-1 block text-sm">{t("contact")}</label>
                <input
                  className="input"
                  value={form.contactName}
                  onChange={(e) =>
                    setForm({ ...form, contactName: e.target.value })
                  }
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="mb-1 block text-sm">{t("phone")}</label>
                  <input
                    className="input"
                    value={form.phone}
                    onChange={(e) => setForm({ ...form, phone: e.target.value })}
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm">{t("email")}</label>
                  <input
                    className="input"
                    value={form.email}
                    onChange={(e) => setForm({ ...form, email: e.target.value })}
                  />
                </div>
              </div>
              <div>
                <label className="mb-1 block text-sm">{t("address")}</label>
                <textarea
                  className="input"
                  rows={2}
                  value={form.address}
                  onChange={(e) => setForm({ ...form, address: e.target.value })}
                />
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
