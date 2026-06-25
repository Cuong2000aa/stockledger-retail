"use client";

import { AsyncSearchSelect } from "@/components/AsyncSearchSelect";
import { LineBarcodeSection } from "@/components/LineBarcodeSection";
import {
  DocumentFormShell,
  DocumentLineCard,
  FormField,
  FormSection,
} from "@/components/document-form";
import { Link, useRouter } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import { useVariantCache } from "@/hooks/useVariantCache";
import {
  createAdjustment,
  createStockCount,
  createStockIn,
  createStockOut,
  createTransfer,
  fetchWarehouses,
} from "@/lib/api";
import { validateInventoryDocumentForm } from "@/lib/validation";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { parseUnitBarcodes } from "@/lib/unitBarcode";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { use, useState, useCallback, useEffect, useMemo } from "react";
import { useInsightPrefill } from "@/features/insights/useInsightPrefill";
import { ListOrdered, Plus, Warehouse } from "lucide-react";

type DocKind =
  | "stock-in"
  | "stock-out"
  | "adjustment"
  | "transfer"
  | "stock-count";

type LineState = {
  productVariantId: string;
  quantity: number;
  adjustmentQuantity: number;
  countedQuantity: number;
  barcodesText: string;
};

const emptyLine = (): LineState => ({
  productVariantId: "",
  quantity: 1,
  adjustmentQuantity: 1,
  countedQuantity: 0,
  barcodesText: "",
});

export default function NewDocumentPage({
  params,
}: {
  params: Promise<{ type: string }>;
}) {
  const { type } = use(params);
  const kind = type as DocKind;
  const t = useTranslations("documents");
  const tCommon = useTranslations("common");
  const router = useRouter();
  const { notifyValidation, notifyError } = useNotify();

  const [warehouseId, setWarehouseId] = useState("");
  const [sourceWarehouseId, setSourceWarehouseId] = useState("");
  const [destinationWarehouseId, setDestinationWarehouseId] = useState("");
  const [reason, setReason] = useState("");
  const [referenceNo, setReferenceNo] = useState("");
  const [note, setNote] = useState("");
  const [lines, setLines] = useState<LineState[]>([emptyLine()]);
  const [warehouseSearch, setWarehouseSearch] = useState("");

  const applyPrefill = useCallback(
    (values: Partial<{
      warehouseId: string;
      sourceWarehouseId: string;
      destinationWarehouseId: string;
      productVariantId: string;
      quantity: number;
      note: string;
      referenceNo: string;
    }>) => {
      if (values.warehouseId) setWarehouseId(values.warehouseId);
      if (values.sourceWarehouseId) setSourceWarehouseId(values.sourceWarehouseId);
      if (values.destinationWarehouseId) {
        setDestinationWarehouseId(values.destinationWarehouseId);
      }
      if (values.note) setNote(values.note);
      if (values.referenceNo) setReferenceNo(values.referenceNo);
      if (values.productVariantId || values.quantity) {
        setLines([
          {
            productVariantId: values.productVariantId ?? "",
            quantity: values.quantity ?? 1,
            adjustmentQuantity: values.quantity ?? 1,
            countedQuantity: values.quantity ?? 0,
            barcodesText: "",
          },
        ]);
      }
    },
    []
  );

  useInsightPrefill(applyPrefill);

  const [debouncedWarehouseSearch, setDebouncedWarehouseSearch] = useState("");
  useEffect(() => {
    const timer = window.setTimeout(
      () => setDebouncedWarehouseSearch(warehouseSearch.trim()),
      300
    );
    return () => window.clearTimeout(timer);
  }, [warehouseSearch]);

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-doc", debouncedWarehouseSearch],
    queryFn: () =>
      fetchWarehouses(1, 100, debouncedWarehouseSearch || undefined),
    staleTime: 60_000,
  });

  const lineVariantIds = useMemo(
    () => lines.map((line) => line.productVariantId),
    [lines]
  );
  const { variantById, loadVariantOptions, variants } =
    useVariantCache(lineVariantIds);

  const titleMap: Record<DocKind, string> = {
    "stock-in": t("createStockIn"),
    "stock-out": t("createStockOut"),
    adjustment: t("createAdjustment"),
    transfer: t("createTransfer"),
    "stock-count": t("createStockCount"),
  };

  const title = titleMap[kind] ?? t("title");

  const mutation = useMutation({
    mutationFn: async () => {
      const documentDate = new Date().toISOString();

      if (kind === "stock-in") {
        return createStockIn({
          destinationWarehouseId: warehouseId,
          documentDate,
          referenceNo: referenceNo || undefined,
          note: note || undefined,
          lines: lines.map((l) => ({
            productVariantId: l.productVariantId,
            quantity: l.quantity,
            barcodes: parseUnitBarcodes(l.barcodesText),
          })),
        });
      }
      if (kind === "stock-out") {
        return createStockOut({
          sourceWarehouseId: warehouseId,
          documentDate,
          referenceNo: referenceNo || undefined,
          note: note || undefined,
          lines: lines.map((l) => ({
            productVariantId: l.productVariantId,
            quantity: l.quantity,
            barcodes: parseUnitBarcodes(l.barcodesText),
          })),
        });
      }
      if (kind === "transfer") {
        return createTransfer({
          sourceWarehouseId,
          destinationWarehouseId,
          documentDate,
          referenceNo: referenceNo || undefined,
          note: note || undefined,
          lines: lines.map((l) => ({
            productVariantId: l.productVariantId,
            quantity: l.quantity,
            barcodes: parseUnitBarcodes(l.barcodesText),
          })),
        });
      }
      if (kind === "stock-count") {
        return createStockCount({
          warehouseId,
          documentDate,
          referenceNo: referenceNo || undefined,
          note: note || undefined,
          lines: lines.map((l) => ({
            productVariantId: l.productVariantId,
            countedQuantity: l.countedQuantity,
            barcodes: parseUnitBarcodes(l.barcodesText),
          })),
        });
      }
      return createAdjustment({
        warehouseId,
        reason,
        documentDate,
        referenceNo: referenceNo || undefined,
        note: note || undefined,
        lines: lines.map((l) => ({
          productVariantId: l.productVariantId,
          adjustmentQuantity: l.adjustmentQuantity,
          barcodes: parseUnitBarcodes(l.barcodesText),
        })),
      });
    },
    onSuccess: (doc) => router.push(`/inventory-documents/${doc.id}`),
    onError: notifyError,
  });

  const handleSave = () => {
    const issues = validateInventoryDocumentForm({
      kind,
      warehouseId,
      sourceWarehouseId,
      destinationWarehouseId,
      reason,
      lines,
      hasVariants: variants.length > 0,
      variantById,
    });

    if (notifyValidation(issues)) return;
    mutation.mutate();
  };

  const updateLine = (idx: number, patch: Partial<LineState>) => {
    const next = [...lines];
    const merged = { ...next[idx], ...patch };
    if (patch.productVariantId !== undefined) {
      merged.barcodesText = "";
    }
    next[idx] = merged;
    setLines(next);
  };

  const lineQuantity = (line: LineState) =>
    kind === "adjustment"
      ? line.adjustmentQuantity
      : kind === "stock-count"
        ? line.countedQuantity
        : line.quantity;

  const warehouseLabel =
    kind === "stock-in"
      ? t("destinationWarehouse")
      : kind === "stock-out"
        ? t("sourceWarehouse")
        : t("warehouse");

  const quantityLabel =
    kind === "adjustment"
      ? t("adjustmentQuantity")
      : kind === "stock-count"
        ? t("countedQuantity")
        : t("quantity");

  return (
    <div>
      <PageHeader
        title={title}
        action={
          <Link href="/inventory-documents" className="btn-secondary">
            {tCommon("back")}
          </Link>
        }
      />

      <DocumentFormShell
        footer={
          <>
            <Link href="/inventory-documents" className="btn-secondary">
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
            <FormSection title={tCommon("formGeneralInfo")} icon={Warehouse}>
              <div className="space-y-4">
                {kind === "transfer" ? (
                  <>
                    <FormField label={t("sourceWarehouse")} required>
                      <input
                        type="search"
                        className="input mb-2"
                        placeholder={tCommon("search")}
                        value={warehouseSearch}
                        onChange={(e) => setWarehouseSearch(e.target.value)}
                      />
                      <select
                        className="input"
                        value={sourceWarehouseId}
                        onChange={(e) => setSourceWarehouseId(e.target.value)}
                      >
                        <option value="">—</option>
                        {warehouses?.items.map((w) => (
                          <option key={w.id} value={w.id}>
                            {formatWarehouseOptionLabel(w)}
                          </option>
                        ))}
                      </select>
                    </FormField>
                    <FormField label={t("destinationWarehouse")} required>
                      <select
                        className="input"
                        value={destinationWarehouseId}
                        onChange={(e) =>
                          setDestinationWarehouseId(e.target.value)
                        }
                      >
                        <option value="">—</option>
                        {warehouses?.items.map((w) => (
                          <option key={w.id} value={w.id}>
                            {formatWarehouseOptionLabel(w)}
                          </option>
                        ))}
                      </select>
                    </FormField>
                  </>
                ) : (
                  <FormField label={warehouseLabel} required>
                    <input
                      type="search"
                      className="input mb-2"
                      placeholder={tCommon("search")}
                      value={warehouseSearch}
                      onChange={(e) => setWarehouseSearch(e.target.value)}
                    />
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
                )}

                {kind === "adjustment" && (
                  <FormField label={t("reason")} required>
                    <input
                      className="input"
                      value={reason}
                      onChange={(e) => setReason(e.target.value)}
                    />
                  </FormField>
                )}

                <FormField label={t("referenceNo")}>
                  <input
                    className="input"
                    value={referenceNo}
                    onChange={(e) => setReferenceNo(e.target.value)}
                  />
                </FormField>
                <FormField label={t("note")}>
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
              title={`${t("lines")} *`}
              description={tCommon("formLinesHint")}
              icon={ListOrdered}
              action={
                <button
                  type="button"
                  className="btn-secondary !px-3 !py-1.5 !text-xs"
                  onClick={() => setLines([...lines, emptyLine()])}
                >
                  <Plus className="h-3.5 w-3.5" />
                  {t("addLine")}
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
                        label={t("selectSku")}
                        required
                        className="sm:col-span-8"
                      >
                        <AsyncSearchSelect
                          value={line.productVariantId}
                          onChange={(id) =>
                            updateLine(idx, { productVariantId: id })
                          }
                          placeholder={tCommon("search")}
                          emptyLabel={t("selectSku")}
                          queryKeyPrefix={`doc-variant-${idx}`}
                          fetchOptions={loadVariantOptions}
                          className="input w-full"
                        />
                      </FormField>
                      <FormField
                        label={quantityLabel}
                        required
                        className="sm:col-span-4"
                      >
                        {kind === "adjustment" ? (
                          <input
                            type="number"
                            className="input"
                            value={line.adjustmentQuantity}
                            onChange={(e) =>
                              updateLine(idx, {
                                adjustmentQuantity: Number(e.target.value),
                              })
                            }
                          />
                        ) : kind === "stock-count" ? (
                          <input
                            type="number"
                            min={0}
                            className="input"
                            value={line.countedQuantity}
                            onChange={(e) =>
                              updateLine(idx, {
                                countedQuantity: Number(e.target.value),
                              })
                            }
                          />
                        ) : (
                          <input
                            type="number"
                            min={1}
                            className="input"
                            value={line.quantity}
                            onChange={(e) =>
                              updateLine(idx, {
                                quantity: Number(e.target.value),
                              })
                            }
                          />
                        )}
                      </FormField>
                    </div>
                    <LineBarcodeSection
                      productVariantId={line.productVariantId}
                      variant={variantById.get(line.productVariantId)}
                      quantity={lineQuantity(line)}
                      value={line.barcodesText}
                      onChange={(text) =>
                        updateLine(idx, { barcodesText: text })
                      }
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
