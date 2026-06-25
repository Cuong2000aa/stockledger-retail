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
  createGoodsReceipt,
  fetchApprovedGoodsReceiptsForPo,
  fetchPurchaseOrder,
} from "@/lib/api";
import { validateGoodsReceiptForm } from "@/lib/validation";
import {
  barcodesForReceiveQuantity,
  buildLineReceiptPrefill,
  collectApprovedReceivedBarcodes,
  getRemainingPoBarcodes,
} from "@/lib/poReceiptPrefill";
import { formatUnitBarcodes, parseUnitBarcodes } from "@/lib/unitBarcode";
import { formatNumber } from "@/lib/format";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { use, useEffect, useMemo, useRef, useState } from "react";
import { FileText, Info, ListOrdered, Package } from "lucide-react";

export default function ReceivePurchaseOrderPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const t = useTranslations("purchaseOrders");
  const tGr = useTranslations("goodsReceipts");
  const tDoc = useTranslations("documents");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const router = useRouter();
  const { notifyValidation, notifyError } = useNotify();

  const [referenceNo, setReferenceNo] = useState("");
  const [note, setNote] = useState("");

  const { data: po, isLoading } = useQuery({
    queryKey: ["purchase-order", id],
    queryFn: () => fetchPurchaseOrder(id),
  });

  const { data: approvedReceipts, isLoading: isLoadingReceipts } = useQuery({
    queryKey: ["goods-receipts-approved-for-po", id],
    queryFn: () => fetchApprovedGoodsReceiptsForPo(id),
    enabled: Boolean(po),
  });

  const receivedBarcodesByLine = useMemo(
    () => collectApprovedReceivedBarcodes(approvedReceipts ?? []),
    [approvedReceipts]
  );

  const receivableLines = useMemo(
    () => po?.lines.filter((l) => l.remainingQuantity > 0) ?? [],
    [po]
  );

  const [qtyByLine, setQtyByLine] = useState<Record<string, number>>({});
  const [barcodesTextByLine, setBarcodesTextByLine] = useState<
    Record<string, string>
  >({});
  const prefillAppliedForPo = useRef<string | null>(null);

  useEffect(() => {
    if (!po || isLoadingReceipts || prefillAppliedForPo.current === po.id) {
      return;
    }

    const qty: Record<string, number> = {};
    const barcodes: Record<string, string> = {};

    for (const line of po.lines) {
      if (line.remainingQuantity <= 0) {
        continue;
      }

      const received = receivedBarcodesByLine.get(line.id);
      const prefill = buildLineReceiptPrefill(line, received);
      qty[line.id] = prefill.quantity;
      if (prefill.barcodesText) {
        barcodes[line.id] = prefill.barcodesText;
      }
    }

    setQtyByLine(qty);
    setBarcodesTextByLine(barcodes);
    prefillAppliedForPo.current = po.id;
  }, [po, isLoadingReceipts, receivedBarcodesByLine]);

  const lineVariantIds = useMemo(
    () => receivableLines.map((line) => line.productVariantId),
    [receivableLines]
  );
  const { variantById } = useVariantCache(lineVariantIds);

  const mutation = useMutation({
    mutationFn: () => {
      const lines = receivableLines
        .map((l) => ({
          purchaseOrderLineId: l.id,
          receivedQuantity: qtyByLine[l.id] ?? 0,
          barcodes: parseUnitBarcodes(barcodesTextByLine[l.id] ?? ""),
        }))
        .filter((l) => l.receivedQuantity > 0);

      return createGoodsReceipt({
        purchaseOrderId: id,
        receiptDate: new Date().toISOString(),
        referenceNo: referenceNo || undefined,
        note: note || undefined,
        lines,
      });
    },
    onSuccess: (gr) => router.push(`/goods-receipts/${gr.id}`),
    onError: notifyError,
  });

  const handleSave = () => {
    const receiptLines = receivableLines.map((line) => ({
      productVariantId: line.productVariantId,
      receivedQuantity: qtyByLine[line.id] ?? 0,
      barcodesText: barcodesTextByLine[line.id],
    }));

    if (
      notifyValidation(
        validateGoodsReceiptForm({ lines: receiptLines, variantById })
      )
    ) {
      return;
    }

    mutation.mutate();
  };

  const applyPoBarcodes = (lineId: string, quantity: number) => {
    const line = receivableLines.find((l) => l.id === lineId);
    if (!line) {
      return;
    }

    const received = receivedBarcodesByLine.get(lineId);
    const remaining = getRemainingPoBarcodes(line, received);
    const slice = barcodesForReceiveQuantity(
      remaining,
      quantity > 0 ? quantity : line.remainingQuantity
    );

    setBarcodesTextByLine((prev) => ({
      ...prev,
      [lineId]: formatUnitBarcodes(slice),
    }));
  };

  if (isLoading || !po || isLoadingReceipts) {
    return <p className="text-slate-500">{tCommon("loading")}</p>;
  }

  const showPrefillHint = receivableLines.length > 0;

  return (
    <div>
      <PageHeader
        title={`${t("receive")}: ${po.poNo}`}
        action={
          <Link href={`/purchase-orders/${id}`} className="btn-secondary">
            {tCommon("back")}
          </Link>
        }
      />

      <DocumentFormShell
        footer={
          <>
            <Link href={`/purchase-orders/${id}`} className="btn-secondary">
              {tCommon("cancel")}
            </Link>
            <button
              className="btn-primary"
              disabled={mutation.isPending || receivableLines.length === 0}
              onClick={handleSave}
            >
              {tGr("title")}
            </button>
          </>
        }
      >
        <div className="grid gap-6 lg:grid-cols-5">
          <div className="lg:col-span-2">
            <FormSection title={tCommon("formGeneralInfo")} icon={FileText}>
              <div className="space-y-4">
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
              title={tDoc("lines")}
              description={tCommon("formLinesHint")}
              icon={ListOrdered}
            >
              {showPrefillHint && (
                <div className="mb-4 flex items-start gap-2 rounded-xl border border-sky-100 bg-sky-50/80 px-3 py-2.5 text-xs text-sky-900">
                  <Info className="mt-0.5 h-3.5 w-3.5 shrink-0 text-sky-600" />
                  <span>{tGr("prefillFromPoHint")}</span>
                </div>
              )}
              {receivableLines.length === 0 ? (
                <p className="py-6 text-center text-sm text-slate-500">
                  {tCommon("noData")}
                </p>
              ) : (
                <div className="space-y-3">
                  {receivableLines.map((line, idx) => {
                    const qty = qtyByLine[line.id] ?? 0;
                    const received = receivedBarcodesByLine.get(line.id);
                    const remainingPoBarcodes = getRemainingPoBarcodes(
                      line,
                      received
                    );
                    const effectiveQty =
                      qty > 0 ? qty : line.remainingQuantity;

                    return (
                      <DocumentLineCard key={line.id} index={idx + 1}>
                        <div className="flex flex-wrap items-center gap-2 rounded-lg bg-white px-3 py-2 ring-1 ring-slate-100">
                          <Package className="h-4 w-4 text-slate-400" />
                          <span className="font-mono text-sm font-semibold text-slate-900">
                            {line.sku}
                          </span>
                          <span className="text-xs text-slate-500">
                            {t("remainingQty")}:{" "}
                            {formatNumber(line.remainingQuantity, locale)}
                          </span>
                        </div>
                        <FormField label={tGr("receivedQty")} required>
                          <input
                            type="number"
                            min={0}
                            max={line.remainingQuantity}
                            className="input max-w-[160px]"
                            value={qtyByLine[line.id] ?? ""}
                            onChange={(e) =>
                              setQtyByLine({
                                ...qtyByLine,
                                [line.id]: Number(e.target.value),
                              })
                            }
                          />
                        </FormField>
                        <LineBarcodeSection
                          productVariantId={line.productVariantId}
                          variant={variantById.get(line.productVariantId)}
                          quantity={effectiveQty}
                          value={barcodesTextByLine[line.id] ?? ""}
                          onChange={(text) =>
                            setBarcodesTextByLine({
                              ...barcodesTextByLine,
                              [line.id]: text,
                            })
                          }
                          poBarcodesRemaining={remainingPoBarcodes}
                          onApplyPoBarcodes={() =>
                            applyPoBarcodes(line.id, qty)
                          }
                        />
                      </DocumentLineCard>
                    );
                  })}
                </div>
              )}
            </FormSection>
          </div>
        </div>
      </DocumentFormShell>
    </div>
  );
}
