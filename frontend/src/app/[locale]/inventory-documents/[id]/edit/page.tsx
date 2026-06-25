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
import { fetchInventoryDocument, updateDocumentDraft } from "@/lib/api";
import { formatVariantOptionLabel } from "@/lib/formatVariantLabel";
import {
  InventoryDocumentStatus,
  InventoryDocumentType,
} from "@/lib/types";
import {
  type InventoryDocumentKind,
  validateInventoryDocumentDraftLines,
} from "@/lib/validation";
import { formatUnitBarcodes, parseUnitBarcodes } from "@/lib/unitBarcode";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { use, useEffect, useState, useMemo } from "react";
import { FileText, ListOrdered, Plus } from "lucide-react";

type LineState = {
  productVariantId: string;
  quantity: number;
  barcodesText: string;
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

  useEffect(() => {
    if (!doc || initialized) return;
    setReferenceNo(doc.referenceNo ?? "");
    setNote(doc.note ?? "");
    setLines(
      doc.lines.map((l) => ({
        productVariantId: l.productVariantId,
        quantity: l.quantity,
        barcodesText: formatUnitBarcodes(l.barcodes ?? []),
      }))
    );
    setInitialized(true);
  }, [doc, initialized]);

  const isStockCount = doc?.documentType === InventoryDocumentType.StockCount;
  const isAdjustment = doc?.documentType === InventoryDocumentType.Adjustment;

  const lineVariantIds = useMemo(
    () => lines.map((line) => line.productVariantId),
    [lines]
  );
  const { variantById, variants } = useVariantCache(lineVariantIds);
  const hasVariants = variants.length > 0;

  const mutation = useMutation({
    mutationFn: () =>
      updateDocumentDraft(id, {
        referenceNo: referenceNo || undefined,
        note: note || undefined,
        lines: lines.map((l) => ({
          productVariantId: l.productVariantId,
          quantity: l.quantity,
          barcodes: parseUnitBarcodes(l.barcodesText),
        })),
      }),
    onSuccess: () => router.push(`/inventory-documents/${id}`),
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
      barcodesText: line.barcodesText,
    }));

    if (
      notifyValidation(
        validateInventoryDocumentDraftLines({
          kind,
          lines: validationLines,
          hasVariants,
          variantById,
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
        <Link
          href={`/inventory-documents/${id}`}
          className="btn-secondary mt-4 inline-block"
        >
          {tCommon("back")}
        </Link>
      </div>
    );
  }

  const updateLine = (idx: number, patch: Partial<LineState>) => {
    const next = [...lines];
    const merged = { ...next[idx], ...patch };
    if (patch.productVariantId !== undefined) {
      merged.barcodesText = "";
    }
    next[idx] = merged;
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

      <DocumentFormShell
        footer={
          <>
            <Link href={`/inventory-documents/${id}`} className="btn-secondary">
              {tCommon("cancel")}
            </Link>
            <button
              className="btn-primary"
              disabled={mutation.isPending || lines.length === 0}
              onClick={handleSave}
            >
              {t("saveDraft")}
            </button>
          </>
        }
      >
        <div className="grid gap-6 lg:grid-cols-5">
          <div className="lg:col-span-2">
            <FormSection title={tCommon("formGeneralInfo")} icon={FileText}>
              <div className="space-y-4">
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
              title={t("lines")}
              description={tCommon("formLinesHint")}
              icon={ListOrdered}
              action={
                <button
                  type="button"
                  className="btn-secondary !px-3 !py-1.5 !text-xs"
                  onClick={() =>
                    setLines([
                      ...lines,
                      {
                        productVariantId: variants[0]?.id ?? "",
                        quantity: isStockCount ? 0 : 1,
                        barcodesText: "",
                      },
                    ])
                  }
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
                        <select
                          className="input"
                          value={line.productVariantId}
                          onChange={(e) =>
                            updateLine(idx, { productVariantId: e.target.value })
                          }
                        >
                          <option value="">{t("selectSku")}</option>
                          {variants.map((v) => (
                            <option key={v.id} value={v.id}>
                              {formatVariantOptionLabel(v)}
                            </option>
                          ))}
                        </select>
                      </FormField>
                      <FormField
                        label={quantityLabel}
                        required
                        className="sm:col-span-4"
                      >
                        <input
                          type="number"
                          min={isStockCount ? 0 : isAdjustment ? undefined : 1}
                          className="input"
                          value={line.quantity}
                          onChange={(e) =>
                            updateLine(idx, { quantity: Number(e.target.value) })
                          }
                        />
                      </FormField>
                    </div>
                    <LineBarcodeSection
                      productVariantId={line.productVariantId}
                      variant={variantById.get(line.productVariantId)}
                      quantity={line.quantity}
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
