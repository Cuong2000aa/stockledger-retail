"use client";

import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { FormModal } from "@/components/FormModal";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton, StatCardsSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
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
import { Mail, Phone, Plus, Truck, UserRound } from "lucide-react";
import { useMemo, useState } from "react";

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

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    return {
      total: data?.totalCount ?? 0,
      active: items.filter((s) => s.status === SupplierStatus.Active).length,
      withContact: items.filter((s) => s.contactName || s.phone || s.email).length,
    };
  }, [data?.items, data?.totalCount]);

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
            <Plus className="h-4 w-4" />
            {t("create")}
          </button>
        }
      />

      {isLoading && !data ? (
        <StatCardsSkeleton />
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <StatCard label={t("stats.total")} value={String(stats.total)} icon={Truck} accent="indigo" />
          <StatCard label={t("stats.active")} value={String(stats.active)} icon={UserRound} accent="emerald" />
          <StatCard label={t("stats.withContact")} value={String(stats.withContact)} icon={Phone} accent="sky" />
        </div>
      )}

      <ListFilterBar
        variant="enhanced"
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchSupplier")}
        onReset={resetSearch}
        showReset={hasSearch}
      />

      <DataTableCard
        title={t("title")}
        icon={Truck}
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
                    <th>{t("contact")}</th>
                    <th>{t("phone")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((s) => (
                    <tr key={s.id} className="hover:bg-slate-50/80">
                      <td><CodePill>{s.code}</CodePill></td>
                      <td className="font-medium text-slate-900">{s.name}</td>
                      <td className="text-sm">{s.contactName ?? "—"}</td>
                      <td>
                        {s.phone ? (
                          <span className="inline-flex items-center gap-1 text-sm text-slate-600">
                            <Phone className="h-3.5 w-3.5" />
                            {s.phone}
                          </span>
                        ) : (
                          "—"
                        )}
                      </td>
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
                      <td className="space-x-2 whitespace-nowrap">
                        <button
                          className="text-sm font-medium text-brand-600 hover:text-brand-700"
                          onClick={() => openEdit(s)}
                        >
                          {tCommon("edit")}
                        </button>
                        <button
                          className="text-sm font-medium text-red-600 hover:text-red-700"
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
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">{t("code")} *</label>
              <input
                className="input"
                value={form.code}
                onChange={(e) => setForm({ ...form, code: e.target.value })}
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
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">{t("contact")}</label>
            <input
              className="input"
              value={form.contactName}
              onChange={(e) => setForm({ ...form, contactName: e.target.value })}
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">{t("phone")}</label>
              <input
                className="input"
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
              />
            </div>
            <div>
              <label className="mb-1 flex items-center gap-1 text-sm font-medium text-slate-700">
                <Mail className="h-3.5 w-3.5" />
                {t("email")}
              </label>
              <input
                className="input"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
              />
            </div>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">{t("address")}</label>
            <textarea
              className="input"
              rows={2}
              value={form.address}
              onChange={(e) => setForm({ ...form, address: e.target.value })}
            />
          </div>
        </div>
      </FormModal>
    </div>
  );
}
