"use client";

import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { FormModal } from "@/components/FormModal";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton, StatCardsSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { StatCard } from "@/components/StatCard";
import {
  ActiveBadge,
  WarehouseTypeBadge,
  isWarehouseActive,
  warehouseTypeKey,
} from "@/components/StatusBadge";
import { useListSearch } from "@/hooks/useListSearch";
import { useNotify } from "@/hooks/useNotify";
import {
  createWarehouse,
  deleteWarehouse,
  fetchWarehouses,
  updateWarehouse,
} from "@/lib/api";
import {
  formatWarehouseLocationSummary,
  formatWarehouseOptionLabel,
} from "@/lib/formatWarehouseAddress";
import { validateWarehouseForm } from "@/lib/validation";
import {
  WarehouseStatus,
  WarehouseType,
  type Warehouse,
} from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Building2, MapPin, Plus, Store, Warehouse as WarehouseIcon } from "lucide-react";
import { useMemo, useState } from "react";

const emptyForm = {
  code: "",
  name: "",
  type: WarehouseType.Store,
  parentWarehouseId: "",
  status: WarehouseStatus.Active,
  addressLine: "",
  ward: "",
  district: "",
  province: "",
  postalCode: "",
  phone: "",
  contactName: "",
};

function needsVietnameseAddress(type: WarehouseType): boolean {
  return type === WarehouseType.Dc || type === WarehouseType.Store;
}

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
  const [form, setForm] = useState(emptyForm);

  const addressRequired = needsVietnameseAddress(form.type);

  const { data, isLoading } = useQuery({
    queryKey: ["warehouses", page, debouncedSearch],
    queryFn: () => fetchWarehouses(page, 20, debouncedSearch || undefined),
  });

  const stats = useMemo(() => {
    const items = data?.items ?? [];
    return {
      total: data?.totalCount ?? 0,
      active: items.filter((w) => isWarehouseActive(w.status)).length,
      stores: items.filter((w) => w.type === WarehouseType.Store).length,
    };
  }, [data?.items, data?.totalCount]);

  const addressPayload = {
    addressLine: form.addressLine || undefined,
    ward: form.ward || undefined,
    district: form.district || undefined,
    province: form.province || undefined,
    postalCode: form.postalCode || undefined,
    phone: form.phone || undefined,
    contactName: form.contactName || undefined,
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      const parentId = form.parentWarehouseId || undefined;
      if (editing) {
        return updateWarehouse(editing.id, {
          name: form.name,
          type: form.type,
          parentWarehouseId: parentId,
          status: form.status,
          ...addressPayload,
        });
      }
      return createWarehouse({
        code: form.code,
        name: form.name,
        type: form.type,
        parentWarehouseId: parentId,
        status: form.status,
        ...addressPayload,
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
    setForm(emptyForm);
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
      addressLine: w.addressLine ?? "",
      ward: w.ward ?? "",
      district: w.district ?? "",
      province: w.province ?? "",
      postalCode: w.postalCode ?? "",
      phone: w.phone ?? "",
      contactName: w.contactName ?? "",
    });
    setModalOpen(true);
  }

  function renderLocation(w: Warehouse) {
    const text =
      w.fullAddress ||
      formatWarehouseLocationSummary(w) ||
      (w.phone ? w.phone : "—");
    return <span className="text-sm text-slate-600">{text}</span>;
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
          <StatCard label={t("stats.total")} value={String(stats.total)} icon={WarehouseIcon} accent="indigo" />
          <StatCard label={t("stats.active")} value={String(stats.active)} icon={Building2} accent="emerald" />
          <StatCard label={t("stats.stores")} value={String(stats.stores)} icon={Store} accent="sky" />
        </div>
      )}

      <ListFilterBar
        variant="enhanced"
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder={tFilters("searchWarehouse")}
        onReset={resetSearch}
        showReset={hasSearch}
      />

      <DataTableCard
        title={t("title")}
        icon={WarehouseIcon}
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
                    <th>{t("type")}</th>
                    <th>{t("location")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((w) => (
                    <tr key={w.id} className="hover:bg-slate-50/80">
                      <td><CodePill>{w.code}</CodePill></td>
                      <td className="font-medium text-slate-900">{w.name}</td>
                      <td>
                        <WarehouseTypeBadge
                          type={w.type}
                          label={tTypes(warehouseTypeKey(w.type))}
                        />
                      </td>
                      <td className="max-w-xs">
                        <div className="flex items-start gap-1.5">
                          <MapPin className="mt-0.5 h-3.5 w-3.5 shrink-0 text-slate-400" />
                          {renderLocation(w)}
                        </div>
                      </td>
                      <td>
                        <ActiveBadge
                          active={isWarehouseActive(w.status)}
                          label={
                            isWarehouseActive(w.status)
                              ? tCommon("active")
                              : tCommon("inactive")
                          }
                        />
                      </td>
                      <td className="space-x-2 whitespace-nowrap">
                        <button
                          className="text-sm font-medium text-brand-600 hover:text-brand-700"
                          onClick={() => openEdit(w)}
                        >
                          {tCommon("edit")}
                        </button>
                        <button
                          className="text-sm font-medium text-red-600 hover:text-red-700"
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
                        {formatWarehouseOptionLabel(w)}
                      </option>
                    ))}
                </select>
              </div>

              <div className="border-t border-slate-200 pt-3">
                <p className="mb-2 text-sm font-medium text-slate-700">
                  {t("addressSection")}
                  {addressRequired ? " *" : ""}
                </p>
                {addressRequired && (
                  <p className="mb-3 text-xs text-slate-500">
                    {t("addressRequiredHint")}
                  </p>
                )}
                <div className="space-y-3">
                  <div>
                    <label className="mb-1 block text-sm">
                      {t("addressLine")}
                      {addressRequired ? " *" : ""}
                    </label>
                    <input
                      className="input"
                      placeholder={t("addressLinePlaceholder")}
                      value={form.addressLine}
                      onChange={(e) =>
                        setForm({ ...form, addressLine: e.target.value })
                      }
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="mb-1 block text-sm">
                        {t("ward")}
                        {addressRequired ? " *" : ""}
                      </label>
                      <input
                        className="input"
                        value={form.ward}
                        onChange={(e) =>
                          setForm({ ...form, ward: e.target.value })
                        }
                      />
                    </div>
                    <div>
                      <label className="mb-1 block text-sm">
                        {t("district")}
                        {addressRequired ? " *" : ""}
                      </label>
                      <input
                        className="input"
                        value={form.district}
                        onChange={(e) =>
                          setForm({ ...form, district: e.target.value })
                        }
                      />
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="mb-1 block text-sm">
                        {t("province")}
                        {addressRequired ? " *" : ""}
                      </label>
                      <input
                        className="input"
                        value={form.province}
                        onChange={(e) =>
                          setForm({ ...form, province: e.target.value })
                        }
                      />
                    </div>
                    <div>
                      <label className="mb-1 block text-sm">{t("postalCode")}</label>
                      <input
                        className="input"
                        value={form.postalCode}
                        onChange={(e) =>
                          setForm({ ...form, postalCode: e.target.value })
                        }
                      />
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="mb-1 block text-sm">{t("phone")}</label>
                      <input
                        className="input"
                        value={form.phone}
                        onChange={(e) =>
                          setForm({ ...form, phone: e.target.value })
                        }
                      />
                    </div>
                    <div>
                      <label className="mb-1 block text-sm">{t("contactName")}</label>
                      <input
                        className="input"
                        value={form.contactName}
                        onChange={(e) =>
                          setForm({ ...form, contactName: e.target.value })
                        }
                      />
                    </div>
                  </div>
                </div>
              </div>
            </div>
      </FormModal>
    </div>
  );
}
