"use client";

import { Link, useRouter } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import {
  createAdjustment,
  createStockIn,
  createStockOut,
  fetchProductVariants,
  fetchWarehouses,
  getApiErrorMessage,
} from "@/lib/api";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { use, useState } from "react";

type DocKind = "stock-in" | "stock-out" | "adjustment";

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

  const [warehouseId, setWarehouseId] = useState("");
  const [reason, setReason] = useState("");
  const [referenceNo, setReferenceNo] = useState("");
  const [note, setNote] = useState("");
  const [lines, setLines] = useState([
    { productVariantId: "", quantity: 1, adjustmentQuantity: 1 },
  ]);
  const [error, setError] = useState<string | null>(null);

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-all"],
    queryFn: () => fetchWarehouses(1, 100),
  });

  const { data: variants } = useQuery({
    queryKey: ["variants-all"],
    queryFn: () => fetchProductVariants(1, 200),
  });

  const title =
    kind === "stock-in"
      ? t("createStockIn")
      : kind === "stock-out"
        ? t("createStockOut")
        : t("createAdjustment");

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
    onError: (e) => setError(getApiErrorMessage(e)),
  });

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
        {error && (
          <p className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">
            {error}
          </p>
        )}

        <div className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium">
              {kind === "stock-in"
                ? t("destinationWarehouse")
                : kind === "stock-out"
                  ? t("sourceWarehouse")
                  : t("warehouse")}{" "}
              *
            </label>
            <select
              className="input"
              value={warehouseId}
              onChange={(e) => setWarehouseId(e.target.value)}
            >
              <option value="">—</option>
              {warehouses?.items.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.code} — {w.name}
                </option>
              ))}
            </select>
          </div>

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
              <span className="text-sm font-medium">{t("lines")}</span>
              <button
                type="button"
                className="text-sm text-brand-600 hover:underline"
                onClick={() =>
                  setLines([
                    ...lines,
                    {
                      productVariantId: variants?.items[0]?.id ?? "",
                      quantity: 1,
                      adjustmentQuantity: 1,
                    },
                  ])
                }
              >
                + {t("addLine")}
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
                  <option value="">SKU</option>
                  {variants?.items.map((v) => (
                    <option key={v.id} value={v.id}>
                      {v.sku}
                    </option>
                  ))}
                </select>
                {kind === "adjustment" ? (
                  <input
                    type="number"
                    className="input w-32"
                    placeholder={t("adjustmentQuantity")}
                    value={line.adjustmentQuantity}
                    onChange={(e) => {
                      const next = [...lines];
                      next[idx].adjustmentQuantity = Number(e.target.value);
                      setLines(next);
                    }}
                  />
                ) : (
                  <input
                    type="number"
                    min={1}
                    className="input w-28"
                    value={line.quantity}
                    onChange={(e) => {
                      const next = [...lines];
                      next[idx].quantity = Number(e.target.value);
                      setLines(next);
                    }}
                  />
                )}
                {lines.length > 1 && (
                  <button
                    type="button"
                    className="text-red-600 text-sm"
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
            disabled={mutation.isPending || !warehouseId}
            onClick={() => mutation.mutate()}
          >
            {tCommon("save")}
          </button>
        </div>
      </div>
    </div>
  );
}
