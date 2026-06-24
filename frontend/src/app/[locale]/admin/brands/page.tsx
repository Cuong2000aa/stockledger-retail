"use client";

import { useAuth } from "@/features/auth/AuthProvider";
import {
  fetchBrands,
  createBrand,
  updateBrand,
} from "@/features/admin/api";
import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { FormModal } from "@/components/FormModal";
import { PageHeader } from "@/components/PageHeader";
import { ActiveBadge } from "@/components/StatusBadge";
import { useNotify } from "@/hooks/useNotify";
import { BrandStatus, type Brand } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Plus, Store } from "lucide-react";
import { useEffect, useState } from "react";
import { useRouter } from "@/i18n/routing";

export default function AdminBrandsPage() {
  const t = useTranslations("admin.brands");
  const tCommon = useTranslations("common");
  const { isSystemAdmin, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const qc = useQueryClient();
  const { notifyError } = useNotify();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Brand | null>(null);
  const [form, setForm] = useState({ code: "", name: "", status: BrandStatus.Active });

  useEffect(() => {
    if (!authLoading && !isSystemAdmin) router.replace("/");
  }, [authLoading, isSystemAdmin, router]);

  const { data, isLoading } = useQuery({
    queryKey: ["brands"],
    queryFn: fetchBrands,
    enabled: isSystemAdmin,
  });

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (editing) {
        return updateBrand(editing.id, { name: form.name, status: form.status });
      }
      return createBrand({ code: form.code, name: form.name });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["brands"] });
      setModalOpen(false);
    },
    onError: notifyError,
  });

  function openCreate() {
    setEditing(null);
    setForm({ code: "", name: "", status: BrandStatus.Active });
    setModalOpen(true);
  }

  function openEdit(brand: Brand) {
    setEditing(brand);
    setForm({ code: brand.code, name: brand.name, status: brand.status });
    setModalOpen(true);
  }

  if (authLoading || !isSystemAdmin) return null;

  return (
    <div>
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <button type="button" className="btn-primary" onClick={openCreate}>
            <Plus className="h-4 w-4" />
            {tCommon("create")}
          </button>
        }
      />

      <DataTableCard title={t("title")} icon={Store}>
        {isLoading ? (
          <p className="p-4 text-slate-500">{tCommon("loading")}</p>
        ) : !data?.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{t("code")}</th>
                  <th>{t("name")}</th>
                  <th>{tCommon("status")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.map((brand) => (
                  <tr key={brand.id}>
                    <td><CodePill>{brand.code}</CodePill></td>
                    <td>{brand.name}</td>
                    <td>
                      <ActiveBadge
                        active={brand.status === BrandStatus.Active}
                        label={brand.status === BrandStatus.Active ? tCommon("active") : tCommon("inactive")}
                      />
                    </td>
                    <td className="text-right">
                      <button type="button" className="btn-ghost text-sm" onClick={() => openEdit(brand)}>
                        {tCommon("edit")}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </DataTableCard>

      <FormModal
        open={modalOpen}
        title={editing ? tCommon("edit") : tCommon("create")}
        onClose={() => setModalOpen(false)}
        footer={
          <div className="flex justify-end gap-2">
            <button type="button" className="btn-secondary" onClick={() => setModalOpen(false)}>
              {tCommon("cancel")}
            </button>
            <button
              type="button"
              className="btn-primary"
              disabled={saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              {tCommon("save")}
            </button>
          </div>
        }
      >
        {!editing && (
          <label className="block">
            <span className="label">{t("code")}</span>
            <input
              className="input-field mt-1 w-full"
              value={form.code}
              onChange={(e) => setForm({ ...form, code: e.target.value })}
            />
          </label>
        )}
        <label className="mt-3 block">
          <span className="label">{t("name")}</span>
          <input
            className="input-field mt-1 w-full"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
          />
        </label>
        {editing && (
          <label className="mt-3 block">
            <span className="label">{tCommon("status")}</span>
            <select
              className="input-field mt-1 w-full"
              value={form.status}
              onChange={(e) => setForm({ ...form, status: Number(e.target.value) })}
            >
              <option value={BrandStatus.Active}>{tCommon("active")}</option>
              <option value={BrandStatus.Inactive}>{tCommon("inactive")}</option>
            </select>
          </label>
        )}
      </FormModal>
    </div>
  );
}
