"use client";

import { AsyncSearchSelect } from "@/components/AsyncSearchSelect";
import { Link, useRouter } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import {
  createAdjustment,
  createStockCount,
  createStockIn,
  createStockOut,
  createTransfer,
  fetchProductVariants,
  fetchWarehouses,
} from "@/lib/api";
import { validateInventoryDocumentForm } from "@/lib/validation";
import { formatWarehouseOptionLabel } from "@/lib/formatWarehouseAddress";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { use, useState, useCallback, useEffect } from "react";
import { useInsightPrefill } from "@/features/insights/useInsightPrefill";

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
};

const emptyLine = (): LineState => ({
  productVariantId: "",
  quantity: 1,
  adjustmentQuantity: 1,
  countedQuantity: 0,
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
      if (values.warehouseId) {
        setWarehouseId(values.warehouseId);
      }
      if (values.sourceWarehouseId) {
        setSourceWarehouseId(values.sourceWarehouseId);
      }
      if (values.destinationWarehouseId) {
        setDestinationWarehouseId(values.destinationWarehouseId);
      }
      if (values.note) {
        setNote(values.note);
      }
      if (values.referenceNo) {
        setReferenceNo(values.referenceNo);
      }
      if (values.productVariantId || values.quantity) {
        setLines([
          {
            productVariantId: values.productVariantId ?? "",
            quantity: values.quantity ?? 1,
            adjustmentQuantity: values.quantity ?? 1,
            countedQuantity: values.quantity ?? 0,
          },
        ]);
      }
    },
    []
  );

  useInsightPrefill(applyPrefill);

  const [debouncedWarehouseSearch, setDebouncedWarehouseSearch] = useState("");
  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedWarehouseSearch(warehouseSearch.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [warehouseSearch]);

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-doc", debouncedWarehouseSearch],
    queryFn: () => fetchWarehouses(1, 100, debouncedWarehouseSearch || undefined),
    staleTime: 60_000,
  });

  const loadVariantOptions = useCallback(async (search: string) => {
    const result = await fetchProductVariants(1, 50, search || undefined);
    return result.items.map((v) => ({ id: v.id, label: v.sku }));
  }, []);

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
        })),
      });
    },
    onSuccess: (doc) => {
      router.push(`/inventory-documents/${doc.id}`);
    },
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
      hasVariants: true,
    });

    if (notifyValidation(issues)) {
      return;
    }

    mutation.mutate();
  };

  const updateLine = (idx: number, patch: Partial<LineState>) => {
    const next = [...lines];
    next[idx] = { ...next[idx], ...patch };
    setLines(next);
  };

  const addLine = () => {
    setLines([...lines, emptyLine()]);
  };

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

      <div className="card max-w-3xl p-6">
        <div className="space-y-4">
          {kind === "transfer" ? (
            <>
              <div>
                <label className="mb-1 block text-sm font-medium">
                  {t("sourceWarehouse")} *
                </label>
                <input
                  type="search"
                  className="input mb-1"
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
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium">
                  {t("destinationWarehouse")} *
                </label>
                <select
                  className="input"
                  value={destinationWarehouseId}
                  onChange={(e) => setDestinationWarehouseId(e.target.value)}
                >
                  <option value="">—</option>
                  {warehouses?.items.map((w) => (
                    <option key={w.id} value={w.id}>
                      {formatWarehouseOptionLabel(w)}
                    </option>
                  ))}
                </select>
              </div>
            </>
          ) : (
            <div>
              <label className="mb-1 block text-sm font-medium">
                {kind === "stock-in"
                  ? t("destinationWarehouse")
                  : kind === "stock-out"
                    ? t("sourceWarehouse")
                    : t("warehouse")}{" "}
                *
              </label>
              <input
                type="search"
                className="input mb-1"
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
            </div>
          )}

          {kind === "adjustment" && (
            <div>
              <label className="mb-1 block text-sm font-medium">
                {t("reason")} *
              </label>
              <input
                className="input"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
            </div>
          )}

          <div>
            <label className="mb-1 block text-sm">{t("referenceNo")}</label>
            <input
              className="input"
              value={referenceNo}
              onChange={(e) => setReferenceNo(e.target.value)}
            />
          </div>

          <div>
            <label className="mb-1 block text-sm">{t("note")}</label>
            <textarea
              className="input"
              rows={2}
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
          </div>

          <div>
            <div className="mb-2 flex items-center justify-between">
              <span className="text-sm font-medium">{t("lines")} *</span>
              <button
                type="button"
                className="text-sm text-brand-600 hover:underline"
                onClick={addLine}
              >
                + {t("addLine")}
              </button>
            </div>
            {lines.map((line, idx) => (
              <div
                key={idx}
                className="mb-3 flex flex-wrap gap-2 rounded-lg border border-slate-200 p-3"
              >
                <AsyncSearchSelect
                  value={line.productVariantId}
                  onChange={(id) => updateLine(idx, { productVariantId: id })}
                  placeholder={tCommon("search")}
                  emptyLabel={t("selectSku")}
                  queryKeyPrefix={`doc-variant-${idx}`}
                  fetchOptions={loadVariantOptions}
                  className="input min-w-[200px] flex-1"
                />
                {kind === "adjustment" ? (
                  <input
                    type="number"
                    className="input w-32"
                    placeholder={t("adjustmentQuantity")}
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
                    className="input w-32"
                    placeholder={t("countedQuantity")}
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
                    className="input w-28"
                    value={line.quantity}
                    onChange={(e) =>
                      updateLine(idx, { quantity: Number(e.target.value) })
                    }
                  />
                )}
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
