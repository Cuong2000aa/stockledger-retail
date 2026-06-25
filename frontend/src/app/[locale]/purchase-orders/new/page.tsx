"use client";

import { Link, useRouter } from "@/i18n/routing";
import { LineBarcodeSection } from "@/components/LineBarcodeSection";
import {
  DocumentFormShell,
  DocumentLineCard,
  FormField,
  FormSection,
} from "@/components/document-form";
import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import { useVariantCache } from "@/hooks/useVariantCache";
import {
  createPurchaseOrder,
  fetchSuppliers,
  fetchWarehouses,
} from "@/lib/api";
import { formatVariantOptionLabel } from "@/lib/formatVariantLabel";
import { validatePurchaseOrderForm } from "@/lib/validation";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { parseUnitBarcodes } from "@/lib/unitBarcode";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useState, useCallback, useMemo } from "react";
import { useInsightPrefill } from "@/features/insights/useInsightPrefill";
import { FileText, ListOrdered, Plus } from "lucide-react";

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
    {
      productVariantId: "",
      orderedQuantity: 1,
      unitCost: undefined as number | undefined,
      barcodesText: "",
    },
  ]);

  const applyPrefill = useCallback(
    (values: Partial<{
      warehouseId: string;
      productVariantId: string;
      orderedQuantity: number;
      note: string;
      referenceNo: string;
    }>) => {
      if (values.warehouseId) setWarehouseId(values.warehouseId);
      if (values.note) setNote(values.note);
      if (values.referenceNo) setReferenceNo(values.referenceNo);
      if (values.productVariantId || values.orderedQuantity) {
        setLines([
          {
            productVariantId: values.productVariantId ?? "",
            orderedQuantity: values.orderedQuantity ?? 1,
            unitCost: undefined,
            barcodesText: "",
          },
        ]);
      }
    },
    []
  );

  useInsightPrefill(applyPrefill);

  const { data: suppliers } = useQuery({
    queryKey: ["suppliers-all"],
    queryFn: () => fetchSuppliers(1, 100),
  });
  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-all"],
    queryFn: () => fetchWarehouses(1, 100),
  });

  const lineVariantIds = useMemo(
    () => lines.map((line) => line.productVariantId),
    [lines]
  );
  const { variantById, variants } = useVariantCache(lineVariantIds);

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
          barcodes: parseUnitBarcodes(l.barcodesText),
        })),
      }),
    onSuccess: (po) => router.push(`/purchase-orders/${po.id}`),
    onError: notifyError,
  });

  const updateLine = (
    idx: number,
    patch: Partial<(typeof lines)[number]>
  ) => {
    const next = [...lines];
    const merged = { ...next[idx], ...patch };
    if (patch.productVariantId !== undefined) {
      merged.barcodesText = "";
    }
    next[idx] = merged;
    setLines(next);
  };

  const addLine = () => {
    setLines([
      ...lines,
      {
        productVariantId: variants[0]?.id ?? "",
        orderedQuantity: 1,
        unitCost: undefined,
        barcodesText: "",
      },
    ]);
  };

  const handleSave = () => {
    if (
      notifyValidation(
        validatePurchaseOrderForm({
          supplierId,
          warehouseId,
          lines,
          hasVariants: variants.length > 0,
          variantById,
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

      <DocumentFormShell
        footer={
          <>
            <Link href="/purchase-orders" className="btn-secondary">
              {tCommon("cancel")}
            </Link>
            <button
              className="btn-primary"
              disabled={mutation.isPending}
              onClick={handleSave}
            >
              {tCommon("save")}
            </button>
          </>
        }
      >
        <div className="grid gap-6 lg:grid-cols-5">
          <div className="space-y-6 lg:col-span-2">
            <FormSection title={tCommon("formGeneralInfo")} icon={FileText}>
              <div className="space-y-4">
                <FormField label={t("supplier")} required>
                  <select
                    className="input"
                    value={supplierId}
                    onChange={(e) => setSupplierId(e.target.value)}
                  >
                    <option value="">—</option>
                    {suppliers?.items.map((s) => (
                      <option key={s.id} value={s.id}>
                        {s.code} — {s.name}
                      </option>
                    ))}
                  </select>
                </FormField>
                <FormField label={tDoc("warehouse")} required>
                  <select
                    className="input"
                    value={warehouseId}
                    onChange={(e) => setWarehouseId(e.target.value)}
                  >
                    <option value="">—</option>
                    {warehouses?.items.map((w) => (
                      <option key={w.id} value={w.id}>
                        {formatWarehouseOptionLabel(w)}
                      </option>
                    ))}
                  </select>
                </FormField>
                <FormField label={tDoc("referenceNo")}>
                  <input
                    className="input"
                    value={referenceNo}
                    onChange={(e) => setReferenceNo(e.target.value)}
                  />
                </FormField>
                <FormField label={tDoc("note")}>
                  <textarea
                    className="input min-h-[88px] resize-y"
                    rows={3}
                    value={note}
                    onChange={(e) => setNote(e.target.value)}
                  />
                </FormField>
              </div>
            </FormSection>
          </div>

          <div className="lg:col-span-3">
            <FormSection
              title={`${tDoc("lines")} *`}
              description={tCommon("formLinesHint")}
              icon={ListOrdered}
              action={
                <button
                  type="button"
                  className="btn-secondary !px-3 !py-1.5 !text-xs"
                  onClick={addLine}
                >
                  <Plus className="h-3.5 w-3.5" />
                  {tDoc("addLine")}
                </button>
              }
            >
              <div className="space-y-3">
                {lines.map((line, idx) => (
                  <DocumentLineCard
                    key={idx}
                    index={idx + 1}
                    canRemove={lines.length > 1}
                    onRemove={() => setLines(lines.filter((_, i) => i !== idx))}
                  >
                    <div className="grid gap-3 sm:grid-cols-12">
                      <FormField
                        label={tDoc("selectSku")}
                        required
                        className="sm:col-span-6"
                      >
                        <select
                          className="input"
                          value={line.productVariantId}
                          onChange={(e) =>
                            updateLine(idx, { productVariantId: e.target.value })
                          }
                        >
                          <option value="">{tDoc("selectSku")}</option>
                          {variants.map((v) => (
                            <option key={v.id} value={v.id}>
                              {formatVariantOptionLabel(v)}
                            </option>
                          ))}
                        </select>
                      </FormField>
                      <FormField
                        label={t("orderedQty")}
                        required
                        className="sm:col-span-3"
                      >
                        <input
                          type="number"
                          min={1}
                          className="input"
                          value={line.orderedQuantity}
                          onChange={(e) =>
                            updateLine(idx, {
                              orderedQuantity: Number(e.target.value),
                            })
                          }
                        />
                      </FormField>
                      <FormField label={t("unitCost")} className="sm:col-span-3">
                        <input
                          type="number"
                          min={0}
                          className="input"
                          placeholder="—"
                          value={line.unitCost ?? ""}
                          onChange={(e) =>
                            updateLine(idx, {
                              unitCost: e.target.value
                                ? Number(e.target.value)
                                : undefined,
                            })
                          }
                        />
                      </FormField>
                    </div>
                    <LineBarcodeSection
                      productVariantId={line.productVariantId}
                      variant={variantById.get(line.productVariantId)}
                      quantity={line.orderedQuantity}
                      value={line.barcodesText}
                      onChange={(text) => updateLine(idx, { barcodesText: text })}
                    />
                  </DocumentLineCard>
                ))}
              </div>
            </FormSection>
          </div>
        </div>
      </DocumentFormShell>
    </div>
  );
}
