"use client";

import { Link, useRouter } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import {
  createGoodsReceipt,
  fetchPurchaseOrder,
  getApiErrorMessage,
} from "@/lib/api";
import { formatNumber } from "@/lib/format";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { use, useMemo, useState } from "react";

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

  const [referenceNo, setReferenceNo] = useState("");
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);

  const { data: po, isLoading } = useQuery({
    queryKey: ["purchase-order", id],
    queryFn: () => fetchPurchaseOrder(id),
  });

  const receivableLines = useMemo(
    () => po?.lines.filter((l) => l.remainingQuantity > 0) ?? [],
    [po]
  );

  const [qtyByLine, setQtyByLine] = useState<Record<string, number>>({});

  const mutation = useMutation({
    mutationFn: () => {
      const lines = receivableLines
        .map((l) => ({
          purchaseOrderLineId: l.id,
          receivedQuantity: qtyByLine[l.id] ?? 0,
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
    onError: (e) => setError(getApiErrorMessage(e)),
  });

  if (isLoading || !po) {
    return <p className="text-slate-500">{tCommon("loading")}</p>;
  }

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

      <div className="card max-w-3xl p-6">
        {error && (
          <p className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</p>
        )}
        <div className="space-y-4">
          <div>
            <label className="mb-1 block text-sm">{tDoc("referenceNo")}</label>
            <input className="input" value={referenceNo} onChange={(e) => setReferenceNo(e.target.value)} />
          </div>
          <div>
            <label className="mb-1 block text-sm">{tDoc("note")}</label>
            <textarea className="input" rows={2} value={note} onChange={(e) => setNote(e.target.value)} />
          </div>

          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tDoc("productVariant")}</th>
                  <th>{t("remainingQty")}</th>
                  <th>{tGr("receivedQty")}</th>
                </tr>
              </thead>
              <tbody>
                {receivableLines.map((line) => (
                  <tr key={line.id}>
                    <td className="font-mono text-xs">{line.sku}</td>
                    <td>{formatNumber(line.remainingQuantity, locale)}</td>
                    <td>
                      <input
                        type="number"
                        min={0}
                        max={line.remainingQuantity}
                        className="input w-28"
                        value={qtyByLine[line.id] ?? ""}
                        onChange={(e) =>
                          setQtyByLine({
                            ...qtyByLine,
                            [line.id]: Number(e.target.value),
                          })
                        }
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <button
            className="btn-primary"
            disabled={mutation.isPending || receivableLines.length === 0}
            onClick={() => mutation.mutate()}
          >
            {tGr("title")}
          </button>
        </div>
      </div>
    </div>
  );
}
