"use client";

import { useAuth } from "@/features/auth/AuthProvider";
import { fetchPermissionGroups, fetchUsers, updateUser } from "@/features/admin/api";
import { DataTableCard, EmptyTableState } from "@/components/DataTableCard";
import { FormModal } from "@/components/FormModal";
import { PageHeader } from "@/components/PageHeader";
import { ActiveBadge } from "@/components/StatusBadge";
import { useNotify } from "@/hooks/useNotify";
import { fetchWarehouses } from "@/lib/api";
import type { AppUser } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Users } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useRouter } from "@/i18n/routing";

type WarehouseAssignmentForm = {
  warehouseId: string;
  isPrimary: boolean;
};

export default function AdminUsersPage() {
  const t = useTranslations("admin.users");
  const tCommon = useTranslations("common");
  const { isSystemAdmin, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const qc = useQueryClient();
  const { notifyError } = useNotify();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AppUser | null>(null);
  const [form, setForm] = useState({
    displayName: "",
    isActive: true,
    groupCodes: [] as string[],
    warehouseAssignments: [] as WarehouseAssignmentForm[],
  });

  useEffect(() => {
    if (!authLoading && !isSystemAdmin) router.replace("/");
  }, [authLoading, isSystemAdmin, router]);

  const { data, isLoading } = useQuery({
    queryKey: ["admin-users"],
    queryFn: fetchUsers,
    enabled: isSystemAdmin,
  });

  const { data: groups } = useQuery({
    queryKey: ["admin-permission-groups"],
    queryFn: fetchPermissionGroups,
    enabled: isSystemAdmin,
  });

  const { data: warehousesPage } = useQuery({
    queryKey: ["admin-warehouses-picker"],
    queryFn: () => fetchWarehouses(1, 200),
    enabled: isSystemAdmin,
  });

  const warehouses = warehousesPage?.items ?? [];

  const warehouseNameById = useMemo(
    () => new Map(warehouses.map((w) => [w.id, `${w.code} — ${w.name}`])),
    [warehouses]
  );

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!editing) {
        throw new Error("Select a user to edit.");
      }

      const primaryCount = form.warehouseAssignments.filter((x) => x.isPrimary).length;
      if (form.warehouseAssignments.length > 0 && primaryCount !== 1) {
        throw new Error(t("primaryWarehouseRequired"));
      }

      return updateUser(editing.id, {
        displayName: form.displayName.trim(),
        isActive: form.isActive,
        groupCodes: form.groupCodes,
        warehouseAssignments: form.warehouseAssignments,
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin-users"] });
      setModalOpen(false);
    },
    onError: notifyError,
  });

  function openEdit(user: AppUser) {
    setEditing(user);
    setForm({
      displayName: user.displayName,
      isActive: user.isActive,
      groupCodes: [...user.groupCodes],
      warehouseAssignments: (user.warehouseAssignments ?? []).map((x) => ({
        warehouseId: x.warehouseId,
        isPrimary: x.isPrimary,
      })),
    });
    setModalOpen(true);
  }

  function toggleGroup(code: string) {
    setForm((prev) => ({
      ...prev,
      groupCodes: prev.groupCodes.includes(code)
        ? prev.groupCodes.filter((x) => x !== code)
        : [...prev.groupCodes, code],
    }));
  }

  function toggleWarehouse(warehouseId: string) {
    setForm((prev) => {
      const exists = prev.warehouseAssignments.some((x) => x.warehouseId === warehouseId);
      if (exists) {
        const next = prev.warehouseAssignments.filter((x) => x.warehouseId !== warehouseId);
        if (next.length === 1 && !next[0].isPrimary) {
          next[0] = { ...next[0], isPrimary: true };
        }
        return { ...prev, warehouseAssignments: next };
      }

      const next = [
        ...prev.warehouseAssignments,
        { warehouseId, isPrimary: prev.warehouseAssignments.length === 0 },
      ];
      return { ...prev, warehouseAssignments: next };
    });
  }

  function setPrimaryWarehouse(warehouseId: string) {
    setForm((prev) => ({
      ...prev,
      warehouseAssignments: prev.warehouseAssignments.map((x) => ({
        ...x,
        isPrimary: x.warehouseId === warehouseId,
      })),
    }));
  }

  if (authLoading || !isSystemAdmin) return null;

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      <DataTableCard title={t("title")} icon={Users}>
        {isLoading ? (
          <p className="p-4 text-slate-500">{tCommon("loading")}</p>
        ) : !data?.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>{tCommon("status")}</th>
                  <th>Groups</th>
                  <th>{t("warehouses")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.map((u) => (
                  <tr key={u.id}>
                    <td>
                      <div className="font-medium">{u.displayName}</div>
                      <div className="text-xs text-slate-500">{u.email}</div>
                    </td>
                    <td>
                      <ActiveBadge
                        active={u.isActive}
                        label={u.isActive ? tCommon("active") : tCommon("inactive")}
                      />
                    </td>
                    <td className="text-xs">{u.groupCodes.join(", ") || "—"}</td>
                    <td className="text-xs">
                      {(u.warehouseAssignments ?? []).length === 0
                        ? "—"
                        : u.warehouseAssignments!
                            .map((a) => {
                              const label = warehouseNameById.get(a.warehouseId) ?? a.warehouseId;
                              return a.isPrimary ? `${label} (${t("primary")})` : label;
                            })
                            .join(", ")}
                    </td>
                    <td className="text-right">
                      <button type="button" className="btn-ghost text-sm" onClick={() => openEdit(u)}>
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
        title={t("editUser")}
        onClose={() => setModalOpen(false)}
        size="lg"
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
        {editing ? (
          <div className="space-y-4">
            <p className="text-sm text-slate-600">{editing.email}</p>

            <label className="block text-sm">
              <span className="mb-1 block font-medium text-slate-700">{t("displayName")}</span>
              <input
                className="input-field mt-1 w-full"
                value={form.displayName}
                onChange={(e) => setForm((prev) => ({ ...prev, displayName: e.target.value }))}
              />
            </label>

            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
              />
              <span>{tCommon("active")}</span>
            </label>

            <div>
              <p className="mb-2 text-sm font-medium text-slate-700">{t("permissionGroups")}</p>
              <div className="max-h-40 space-y-2 overflow-y-auto rounded-lg border border-slate-200 p-3">
                {(groups ?? []).map((group) => (
                  <label key={group.id} className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={form.groupCodes.includes(group.code)}
                      onChange={() => toggleGroup(group.code)}
                    />
                    <span>{group.name}</span>
                    <span className="text-xs text-slate-400">({group.code})</span>
                  </label>
                ))}
              </div>
            </div>

            <div>
              <p className="mb-1 text-sm font-medium text-slate-700">{t("warehouseAssignments")}</p>
              <p className="mb-2 text-xs text-slate-500">{t("warehouseAssignmentsHint")}</p>
              <div className="max-h-48 space-y-2 overflow-y-auto rounded-lg border border-slate-200 p-3">
                {warehouses.length === 0 ? (
                  <p className="text-sm text-slate-500">{tCommon("noData")}</p>
                ) : (
                  warehouses.map((warehouse) => {
                    const assignment = form.warehouseAssignments.find(
                      (x) => x.warehouseId === warehouse.id
                    );
                    const checked = Boolean(assignment);
                    return (
                      <div key={warehouse.id} className="flex flex-wrap items-center gap-2 text-sm">
                        <label className="flex min-w-0 flex-1 items-center gap-2">
                          <input
                            type="checkbox"
                            checked={checked}
                            onChange={() => toggleWarehouse(warehouse.id)}
                          />
                          <span className="truncate">
                            {warehouse.code} — {warehouse.name}
                          </span>
                        </label>
                        {checked ? (
                          <label className="flex items-center gap-1 text-xs text-slate-600">
                            <input
                              type="radio"
                              name={`primary-${editing.id}`}
                              checked={assignment?.isPrimary ?? false}
                              onChange={() => setPrimaryWarehouse(warehouse.id)}
                            />
                            {t("primary")}
                          </label>
                        ) : null}
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          </div>
        ) : null}
      </FormModal>
    </div>
  );
}
