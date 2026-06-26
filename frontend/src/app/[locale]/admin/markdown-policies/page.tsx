"use client";

import { useAuth } from "@/features/auth/AuthProvider";
import {
  createMarkdownPolicy,
  fetchBrands,
  fetchMarkdownPolicies,
  updateMarkdownPolicy,
} from "@/features/admin/api";
import { DataTableCard, EmptyTableState } from "@/components/DataTableCard";
import { FormModal } from "@/components/FormModal";
import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import type { MarkdownPolicy, MarkdownPolicyTier } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { BadgePercent, Pencil, Plus } from "lucide-react";
import { useEffect, useState } from "react";
import { useRouter } from "@/i18n/routing";

const defaultTiers = (): MarkdownPolicyTier[] => [
  {
    tierCode: "watch",
    minDaysWithoutOutbound: 60,
    maxDaysWithoutOutbound: 89,
    markdownPercent: 10,
    slowSellThroughMarkdownPercent: 15,
    severity: "warning",
  },
  {
    tierCode: "moderate",
    minDaysWithoutOutbound: 90,
    maxDaysWithoutOutbound: 119,
    markdownPercent: 15,
    slowSellThroughMarkdownPercent: 20,
    severity: "warning",
  },
  {
    tierCode: "aggressive",
    minDaysWithoutOutbound: 120,
    markdownPercent: 25,
    slowSellThroughMarkdownPercent: 30,
    severity: "critical",
  },
];

const emptyForm = () => ({
  brandId: "",
  lookbackDays: "30",
  minDaysWithoutOutbound: "60",
  minOnHand: "1",
  minGrossMarginPercent: "15",
  maxMarkdownPercent: "35",
  requireApprovalAbovePercent: "20",
  slowSellThroughThreshold: "0.5",
  allowBelowCost: false,
  note: "",
  tiers: defaultTiers(),
});

function formFromPolicy(policy: MarkdownPolicy) {
  return {
    brandId: policy.brandId,
    lookbackDays: String(policy.lookbackDays),
    minDaysWithoutOutbound: String(policy.minDaysWithoutOutbound),
    minOnHand: String(policy.minOnHand),
    minGrossMarginPercent: String(policy.minGrossMarginPercent),
    maxMarkdownPercent: String(policy.maxMarkdownPercent),
    requireApprovalAbovePercent:
      policy.requireApprovalAbovePercent != null
        ? String(policy.requireApprovalAbovePercent)
        : "",
    slowSellThroughThreshold: String(policy.slowSellThroughThreshold),
    allowBelowCost: policy.allowBelowCost,
    note: policy.note ?? "",
    tiers: policy.tiers.length ? policy.tiers : defaultTiers(),
  };
}

export default function AdminMarkdownPoliciesPage() {
  const t = useTranslations("admin.markdownPolicies");
  const tCommon = useTranslations("common");
  const { isSystemAdmin, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const qc = useQueryClient();
  const { notifyError } = useNotify();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<MarkdownPolicy | null>(null);
  const [form, setForm] = useState(emptyForm());

  useEffect(() => {
    if (!authLoading && !isSystemAdmin) router.replace("/");
  }, [authLoading, isSystemAdmin, router]);

  const { data, isLoading } = useQuery({
    queryKey: ["markdown-policies"],
    queryFn: fetchMarkdownPolicies,
    enabled: isSystemAdmin,
  });

  const { data: brands } = useQuery({
    queryKey: ["brands"],
    queryFn: fetchBrands,
    enabled: isSystemAdmin,
  });

  const buildPayload = () => ({
    lookbackDays: Number(form.lookbackDays) || 30,
    minDaysWithoutOutbound: Number(form.minDaysWithoutOutbound) || 60,
    minOnHand: Number(form.minOnHand) || 1,
    minGrossMarginPercent: Number(form.minGrossMarginPercent) || 0,
    maxMarkdownPercent: Number(form.maxMarkdownPercent) || 50,
    allowBelowCost: form.allowBelowCost,
    requireApprovalAbovePercent: form.requireApprovalAbovePercent
      ? Number(form.requireApprovalAbovePercent)
      : undefined,
    slowSellThroughThreshold: Number(form.slowSellThroughThreshold) || 0.5,
    tiers: form.tiers,
    note: form.note || undefined,
  });

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload = buildPayload();
      if (editing) {
        return updateMarkdownPolicy(editing.id, { ...payload, isActive: editing.isActive });
      }
      return createMarkdownPolicy({ brandId: form.brandId, ...payload });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["markdown-policies"] });
      setModalOpen(false);
      setEditing(null);
      setForm(emptyForm());
    },
    onError: notifyError,
  });

  const toggleMutation = useMutation({
    mutationFn: (policy: MarkdownPolicy) =>
      updateMarkdownPolicy(policy.id, {
        regionCode: policy.regionCode,
        warehouseType: policy.warehouseType,
        lookbackDays: policy.lookbackDays,
        minDaysWithoutOutbound: policy.minDaysWithoutOutbound,
        minOnHand: policy.minOnHand,
        minInventoryValueAtCost: policy.minInventoryValueAtCost,
        minGrossMarginPercent: policy.minGrossMarginPercent,
        maxMarkdownPercent: policy.maxMarkdownPercent,
        allowBelowCost: policy.allowBelowCost,
        requireApprovalAbovePercent: policy.requireApprovalAbovePercent,
        slowSellThroughThreshold: policy.slowSellThroughThreshold,
        tiers: policy.tiers,
        isActive: !policy.isActive,
        note: policy.note,
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["markdown-policies"] }),
    onError: notifyError,
  });

  function openCreate() {
    setEditing(null);
    setForm(emptyForm());
    setModalOpen(true);
  }

  function openEditPolicy(policy: MarkdownPolicy) {
    setEditing(policy);
    setForm(formFromPolicy(policy));
    setModalOpen(true);
  }

  function updateTier(index: number, patch: Partial<MarkdownPolicyTier>) {
    setForm((current) => ({
      ...current,
      tiers: current.tiers.map((tier, i) => (i === index ? { ...tier, ...patch } : tier)),
    }));
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

      <DataTableCard title={t("title")} icon={BadgePercent}>
        {isLoading ? (
          <p className="p-4 text-slate-500">{tCommon("loading")}</p>
        ) : !data?.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{t("brand")}</th>
                  <th>{t("minMargin")}</th>
                  <th>{t("maxMarkdown")}</th>
                  <th>{t("tierCount")}</th>
                  <th>{tCommon("status")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.map((policy) => (
                  <tr key={policy.id}>
                    <td className="font-medium">{policy.brandName ?? policy.brandId}</td>
                    <td className="tabular-nums">{policy.minGrossMarginPercent}%</td>
                    <td className="tabular-nums">{policy.maxMarkdownPercent}%</td>
                    <td className="tabular-nums">{policy.tiers.length}</td>
                    <td>{policy.isActive ? tCommon("active") : tCommon("inactive")}</td>
                    <td className="text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          type="button"
                          className="btn-ghost text-sm"
                          onClick={() => openEditPolicy(policy)}
                        >
                          <Pencil className="mr-1 inline h-3.5 w-3.5" />
                          {tCommon("edit")}
                        </button>
                        <button
                          type="button"
                          className="btn-ghost text-sm"
                          onClick={() => toggleMutation.mutate(policy)}
                        >
                          {policy.isActive ? tCommon("inactive") : tCommon("active")}
                        </button>
                      </div>
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
        onClose={() => {
          setModalOpen(false);
          setEditing(null);
        }}
        footer={
          <div className="flex justify-end gap-2">
            <button type="button" className="btn-secondary" onClick={() => setModalOpen(false)}>
              {tCommon("cancel")}
            </button>
            <button
              type="button"
              className="btn-primary"
              disabled={saveMutation.isPending || (!editing && !form.brandId)}
              onClick={() => saveMutation.mutate()}
            >
              {tCommon("save")}
            </button>
          </div>
        }
      >
        {!editing ? (
          <label className="block">
            <span className="label">{t("brand")}</span>
            <select
              className="input-field mt-1 w-full"
              value={form.brandId}
              onChange={(e) => setForm({ ...form, brandId: e.target.value })}
            >
              <option value="">{t("selectBrand")}</option>
              {brands?.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name}
                </option>
              ))}
            </select>
          </label>
        ) : null}

        <div className="mt-3 grid grid-cols-2 gap-3">
          {[
            ["lookbackDays", t("lookbackDays")],
            ["minDaysWithoutOutbound", t("minDaysIdle")],
            ["minGrossMarginPercent", t("minMargin")],
            ["maxMarkdownPercent", t("maxMarkdown")],
            ["requireApprovalAbovePercent", t("approvalAbove")],
            ["slowSellThroughThreshold", t("slowSellThreshold")],
          ].map(([key, label]) => (
            <label key={key} className="block">
              <span className="label">{label}</span>
              <input
                className="input-field mt-1 w-full"
                value={form[key as keyof typeof form] as string}
                onChange={(e) => setForm({ ...form, [key]: e.target.value })}
              />
            </label>
          ))}
        </div>

        <label className="mt-3 flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={form.allowBelowCost}
            onChange={(e) => setForm({ ...form, allowBelowCost: e.target.checked })}
          />
          {t("allowBelowCost")}
        </label>

        <div className="mt-4 space-y-3">
          <p className="text-sm font-semibold text-slate-900">{t("tiersTitle")}</p>
          {form.tiers.map((tier, index) => (
            <div key={tier.tierCode} className="rounded-xl border border-slate-100 bg-slate-50/70 p-3">
              <p className="mb-2 text-xs font-semibold uppercase text-slate-500">{tier.tierCode}</p>
              <div className="grid grid-cols-2 gap-2">
                <label className="block text-xs">
                  {t("tierMinDays")}
                  <input
                    className="input-field mt-1 w-full"
                    type="number"
                    value={tier.minDaysWithoutOutbound}
                    onChange={(e) =>
                      updateTier(index, { minDaysWithoutOutbound: Number(e.target.value) })
                    }
                  />
                </label>
                <label className="block text-xs">
                  {t("tierMaxDays")}
                  <input
                    className="input-field mt-1 w-full"
                    type="number"
                    value={tier.maxDaysWithoutOutbound ?? ""}
                    onChange={(e) =>
                      updateTier(index, {
                        maxDaysWithoutOutbound: e.target.value ? Number(e.target.value) : undefined,
                      })
                    }
                  />
                </label>
                <label className="block text-xs">
                  {t("tierPercent")}
                  <input
                    className="input-field mt-1 w-full"
                    type="number"
                    value={tier.markdownPercent}
                    onChange={(e) => updateTier(index, { markdownPercent: Number(e.target.value) })}
                  />
                </label>
                <label className="block text-xs">
                  {t("tierSlowPercent")}
                  <input
                    className="input-field mt-1 w-full"
                    type="number"
                    value={tier.slowSellThroughMarkdownPercent ?? ""}
                    onChange={(e) =>
                      updateTier(index, {
                        slowSellThroughMarkdownPercent: e.target.value
                          ? Number(e.target.value)
                          : undefined,
                      })
                    }
                  />
                </label>
              </div>
            </div>
          ))}
        </div>
      </FormModal>
    </div>
  );
}
