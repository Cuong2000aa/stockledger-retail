"use client";

import { Link, useRouter } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import {
  fetchInventoryDocument,
  fetchProductVariants,
  updateDocumentDraft,
} from "@/lib/api";
import {
  InventoryDocumentStatus,
  InventoryDocumentType,
} from "@/lib/types";
import {
  type InventoryDocumentKind,
  validateInventoryDocumentDraftLines,
} from "@/lib/validation";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { use, useEffect, useState } from "react";

type LineState = {
  productVariantId: string;
  quantity: number;
};

function documentTypeToKind(type: InventoryDocumentType): InventoryDocumentKind {
  switch (type) {
    case InventoryDocumentType.StockIn:
      return "stock-in";
    case InventoryDocumentType.StockOut:
      return "stock-out";
    case InventoryDocumentType.Transfer:
      return "transfer";
    case InventoryDocumentType.Adjustment:
      return "adjustment";
    case InventoryDocumentType.StockCount:
      return "stock-count";
    default:
      return "stock-in";
  }
}

export default function EditDocumentPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const t = useTranslations("documents");
  const tCommon = useTranslations("common");
  const router = useRouter();
  const { notifyValidation, notifyError } = useNotify();

  const [referenceNo, setReferenceNo] = useState("");
  const [note, setNote] = useState("");
  const [lines, setLines] = useState<LineState[]>([]);
  const [initialized, setInitialized] = useState(false);

  const { data: doc, isLoading } = useQuery({
    queryKey: ["inventory-document", id],
    queryFn: () => fetchInventoryDocument(id),
  });

  const { data: variants } = useQuery({
    queryKey: ["variants-all"],
    queryFn: () => fetchProductVariants(1, 200),
  });

  useEffect(() => {
    if (!doc || initialized) return;
    setReferenceNo(doc.referenceNo ?? "");
    setNote(doc.note ?? "");
    setLines(
      doc.lines.map((l) => ({
        productVariantId: l.productVariantId,
        quantity: l.quantity,
      }))
    );
    setInitialized(true);
  }, [doc, initialized]);

  const isStockCount = doc?.documentType === InventoryDocumentType.StockCount;
  const isAdjustment = doc?.documentType === InventoryDocumentType.Adjustment;
  const hasVariants = (variants?.items.length ?? 0) > 0;

  const mutation = useMutation({
    mutationFn: () =>
      updateDocumentDraft(id, {
        referenceNo: referenceNo || undefined,
        note: note || undefined,
        lines: lines.map((l) => ({
          productVariantId: l.productVariantId,
          quantity: l.quantity,
        })),
      }),
    onSuccess: () => {
      router.push(`/inventory-documents/${id}`);
    },
    onError: notifyError,
  });

  const handleSave = () => {
    if (!doc) return;

    const kind = documentTypeToKind(doc.documentType);
    const validationLines = lines.map((line) => ({
      productVariantId: line.productVariantId,
      quantity: isStockCount || isAdjustment ? 0 : line.quantity,
      adjustmentQuantity: isAdjustment ? line.quantity : 0,
      countedQuantity: isStockCount ? line.quantity : 0,
    }));

    if (
      notifyValidation(
        validateInventoryDocumentDraftLines({
          kind,
          lines: validationLines,
          hasVariants,
        })
      )
    ) {
      return;
    }

    mutation.mutate();
  };

  if (isLoading || !doc) {
    return <p className="text-slate-500">{tCommon("loading")}</p>;
  }

  if (doc.status !== InventoryDocumentStatus.Draft) {
    return (
      <div>
        <PageHeader title={doc.documentNo} />
        <p className="text-red-600">{t("editDraftOnly")}</p>
        <Link href={`/inventory-documents/${id}`} className="btn-secondary mt-4 inline-block">
          {tCommon("back")}
        </Link>
      </div>
    );
  }

  const updateLine = (idx: number, patch: Partial<LineState>) => {
    const next = [...lines];
    next[idx] = { ...next[idx], ...patch };
    setLines(next);
  };

  const quantityLabel = isStockCount
    ? t("countedQuantity")
    : isAdjustment
      ? t("adjustmentQuantity")
      : t("quantity");

  return (
    <div>
      <PageHeader
        title={`${t("editDraft")}: ${doc.documentNo}`}
        action={
          <Link href={`/inventory-documents/${id}`} className="btn-secondary">
            {tCommon("back")}
          </Link>
        }
      />

      <div className="card max-w-3xl p-6">
        <div className="space-y-4">
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
                      quantity: isStockCount ? 0 : 1,
                    },
                  ])
                }
              >
                + {t("addLine")}
              </button>
            </div>
            {lines.map((line, idx) => (
              <div
                key={idx}
                className="mb-3 flex flex-wrap gap-2 rounded-lg border border-slate-200 p-3"
              >
                <select
                  className="input min-w-[200px] flex-1"
                  value={line.productVariantId}
                  onChange={(e) =>
                    updateLine(idx, { productVariantId: e.target.value })
                  }
                >
                  <option value="">{t("selectSku")}</option>
                  {variants?.items.map((v) => (
                    <option key={v.id} value={v.id}>
                      {v.sku}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  min={isStockCount ? 0 : isAdjustment ? undefined : 1}
                  className="input w-32"
                  placeholder={quantityLabel}
                  value={line.quantity}
                  onChange={(e) =>
                    updateLine(idx, { quantity: Number(e.target.value) })
                  }
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
            disabled={mutation.isPending || lines.length === 0}
            onClick={handleSave}
          >
            {t("saveDraft")}
          </button>
        </div>
      </div>
    </div>
  );
}
