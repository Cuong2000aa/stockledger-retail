"use client";

import { Link, useRouter } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import {
  createPurchaseOrder,
  fetchProductVariants,
  fetchSuppliers,
  fetchWarehouses,
} from "@/lib/api";
import { validatePurchaseOrderForm } from "@/lib/validation";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useState } from "react";

export default function NewPurchaseOrderPage() {
  const t = useTranslations("purchaseOrders");
  const tDoc = useTranslations("documents");
  const tCommon = useTranslations("common");
  const router = useRouter();
  const { notifyValidation, notifyError } = useNotify();

  const [supplierId, setSupplierId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [referenceNo, setReferenceNo] = useState("");
  const [note, setNote] = useState("");
  const [lines, setLines] = useState([
    { productVariantId: "", orderedQuantity: 1, unitCost: undefined as number | undefined },
  ]);

  const { data: suppliers } = useQuery({
    queryKey: ["suppliers-all"],
    queryFn: () => fetchSuppliers(1, 100),
  });
  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-all"],
    queryFn: () => fetchWarehouses(1, 100),
  });
  const { data: variants } = useQuery({
    queryKey: ["variants-all"],
    queryFn: () => fetchProductVariants(1, 200),
  });

  const mutation = useMutation({
    mutationFn: () =>
      createPurchaseOrder({
        supplierId,
        warehouseId,
        orderDate: new Date().toISOString(),
        referenceNo: referenceNo || undefined,
        note: note || undefined,
        lines: lines.map((l) => ({
          productVariantId: l.productVariantId,
          orderedQuantity: l.orderedQuantity,
          unitCost: l.unitCost,
        })),
      }),
    onSuccess: (po) => router.push(`/purchase-orders/${po.id}`),
    onError: notifyError,
  });

  const hasVariants = (variants?.items.length ?? 0) > 0;

  const handleSave = () => {
    if (
      notifyValidation(
        validatePurchaseOrderForm({
          supplierId,
          warehouseId,
          lines,
          hasVariants,
        })
      )
    ) {
      return;
    }
    mutation.mutate();
  };

  return (
    <div>
      <PageHeader
        title={t("create")}
        action={
          <Link href="/purchase-orders" className="btn-secondary">
            {tCommon("back")}
          </Link>
        }
      />

      <div className="card max-w-3xl p-6">
        <div className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium">{t("supplier")} *</label>
            <select className="input" value={supplierId} onChange={(e) => setSupplierId(e.target.value)}>
              <option value="">—</option>
              {suppliers?.items.map((s) => (
                <option key={s.id} value={s.id}>{s.code} — {s.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">{tDoc("warehouse")} *</label>
            <select className="input" value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)}>
              <option value="">—</option>
              {warehouses?.items.map((w) => (
                <option key={w.id} value={w.id}>{w.code} — {w.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm">{tDoc("referenceNo")}</label>
            <input className="input" value={referenceNo} onChange={(e) => setReferenceNo(e.target.value)} />
          </div>
          <div>
            <label className="mb-1 block text-sm">{tDoc("note")}</label>
            <textarea className="input" rows={2} value={note} onChange={(e) => setNote(e.target.value)} />
          </div>
          <div>
            <div className="mb-2 flex justify-between">
              <span className="text-sm font-medium">{tDoc("lines")}</span>
              <button
                type="button"
                className="text-sm text-brand-600 hover:underline"
                onClick={() =>
                  setLines([
                    ...lines,
                    { productVariantId: variants?.items[0]?.id ?? "", orderedQuantity: 1, unitCost: undefined },
                  ])
                }
              >
                + {tDoc("addLine")}
              </button>
            </div>
            {lines.map((line, idx) => (
              <div key={idx} className="mb-3 flex flex-wrap gap-2 rounded-lg border border-slate-200 p-3">
                <select
                  className="input min-w-[200px] flex-1"
                  value={line.productVariantId}
                  onChange={(e) => {
                    const next = [...lines];
                    next[idx].productVariantId = e.target.value;
                    setLines(next);
                  }}
                >
                  <option value="">{tDoc("selectSku")}</option>
                  {variants?.items.map((v) => (
                    <option key={v.id} value={v.id}>{v.sku}</option>
                  ))}
                </select>
                <input
                  type="number"
                  min={1}
                  className="input w-28"
                  placeholder={t("orderedQty")}
                  value={line.orderedQuantity}
                  onChange={(e) => {
                    const next = [...lines];
                    next[idx].orderedQuantity = Number(e.target.value);
                    setLines(next);
                  }}
                />
                <input
                  type="number"
                  min={0}
                  className="input w-28"
                  placeholder={t("unitCost")}
                  value={line.unitCost ?? ""}
                  onChange={(e) => {
                    const next = [...lines];
                    next[idx].unitCost = e.target.value ? Number(e.target.value) : undefined;
                    setLines(next);
                  }}
                />
                {lines.length > 1 && (
                  <button
                    type="button"
                    className="text-sm text-red-600"
                    onClick={() => setLines(lines.filter((_, i) => i !== idx))}
                  >
                    {tCommon("delete")}
                  </button>
                )}
              </div>
            ))}
          </div>
          <button
            className="btn-primary"
            disabled={mutation.isPending}
            onClick={handleSave}
          >
            {tCommon("save")}
          </button>
        </div>
      </div>
    </div>
  );
}
