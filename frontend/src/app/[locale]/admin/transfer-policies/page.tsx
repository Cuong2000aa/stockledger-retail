"use client";

import { useAuth } from "@/features/auth/AuthProvider";
import {
  createTransferPolicy,
  fetchBrands,
  fetchTransferPolicies,
  updateTransferPolicy,
} from "@/features/admin/api";
import { DataTableCard, EmptyTableState } from "@/components/DataTableCard";
import { FormModal } from "@/components/FormModal";
import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ArrowLeftRight, Plus } from "lucide-react";
import { useEffect, useState } from "react";
import { useRouter } from "@/i18n/routing";
import type { TransferPolicy } from "@/lib/types";

export default function AdminTransferPoliciesPage() {
  const t = useTranslations("admin.transferPolicies");
  const tCommon = useTranslations("common");
  const { isSystemAdmin, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const qc = useQueryClient();
  const { notifyError } = useNotify();
  const [modalOpen, setModalOpen] = useState(false);
  const [form, setForm] = useState({
    sourceBrandId: "",
    destinationBrandId: "",
    allowCrossBrand: true,
    note: "",
  });

  useEffect(() => {
    if (!authLoading && !isSystemAdmin) router.replace("/");
  }, [authLoading, isSystemAdmin, router]);

  const { data, isLoading } = useQuery({
    queryKey: ["transfer-policies"],
    queryFn: fetchTransferPolicies,
    enabled: isSystemAdmin,
  });

  const { data: brands } = useQuery({
    queryKey: ["brands"],
    queryFn: fetchBrands,
    enabled: isSystemAdmin,
  });

  const saveMutation = useMutation({
    mutationFn: () =>
      createTransferPolicy({
        sourceBrandId: form.sourceBrandId || undefined,
        destinationBrandId: form.destinationBrandId || undefined,
        allowCrossBrand: form.allowCrossBrand,
        note: form.note || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["transfer-policies"] });
      setModalOpen(false);
    },
    onError: notifyError,
  });

  const toggleMutation = useMutation({
    mutationFn: (policy: TransferPolicy) =>
      updateTransferPolicy(policy.id, {
        allowCrossBrand: policy.allowCrossBrand,
        isActive: !policy.isActive,
        note: policy.note,
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["transfer-policies"] }),
    onError: notifyError,
  });

  if (authLoading || !isSystemAdmin) return null;

  return (
    <div>
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <button type="button" className="btn-primary" onClick={() => setModalOpen(true)}>
            <Plus className="h-4 w-4" />
            {tCommon("create")}
          </button>
        }
      />

      <DataTableCard title={t("title")} icon={ArrowLeftRight}>
        {isLoading ? (
          <p className="p-4 text-slate-500">{tCommon("loading")}</p>
        ) : !data?.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Source</th>
                  <th>Dest</th>
                  <th>Cross-brand</th>
                  <th>{tCommon("status")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.map((p) => (
                  <tr key={p.id}>
                    <td>{p.sourceBrandName ?? "*"}</td>
                    <td>{p.destinationBrandName ?? "*"}</td>
                    <td>{p.allowCrossBrand ? tCommon("yes") : tCommon("no")}</td>
                    <td>{p.isActive ? tCommon("active") : tCommon("inactive")}</td>
                    <td className="text-right">
                      <button
                        type="button"
                        className="btn-ghost text-sm"
                        onClick={() => toggleMutation.mutate(p)}
                      >
                        {p.isActive ? tCommon("inactive") : tCommon("active")}
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
        title={tCommon("create")}
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
        <label className="block">
          <span className="label">Source brand</span>
          <select
            className="input-field mt-1 w-full"
            value={form.sourceBrandId}
            onChange={(e) => setForm({ ...form, sourceBrandId: e.target.value })}
          >
            <option value="">* All</option>
            {brands?.map((b) => (
              <option key={b.id} value={b.id}>{b.name}</option>
            ))}
          </select>
        </label>
        <label className="mt-3 block">
          <span className="label">Destination brand</span>
          <select
            className="input-field mt-1 w-full"
            value={form.destinationBrandId}
            onChange={(e) => setForm({ ...form, destinationBrandId: e.target.value })}
          >
            <option value="">* All</option>
            {brands?.map((b) => (
              <option key={b.id} value={b.id}>{b.name}</option>
            ))}
          </select>
        </label>
      </FormModal>
    </div>
  );
}
