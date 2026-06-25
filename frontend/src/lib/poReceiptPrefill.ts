import type { GoodsReceipt, PurchaseOrderLine } from "./types";
import { GoodsReceiptStatus } from "./types";
import { formatUnitBarcodes } from "./unitBarcode";

/** Barcodes already posted from approved goods receipts, keyed by PO line id. */
export function collectApprovedReceivedBarcodes(
  receipts: GoodsReceipt[]
): Map<string, Set<string>> {
  const byLine = new Map<string, Set<string>>();

  for (const gr of receipts) {
    if (gr.status !== GoodsReceiptStatus.Approved) {
      continue;
    }

    for (const line of gr.lines) {
      let set = byLine.get(line.purchaseOrderLineId);
      if (!set) {
        set = new Set<string>();
        byLine.set(line.purchaseOrderLineId, set);
      }

      for (const barcode of line.barcodes ?? []) {
        set.add(barcode.toLowerCase());
      }
    }
  }

  return byLine;
}

export function getRemainingPoBarcodes(
  poLine: PurchaseOrderLine,
  alreadyReceived?: Set<string>
): string[] {
  const poBarcodes = poLine.barcodes ?? [];
  if (!alreadyReceived?.size) {
    return [...poBarcodes];
  }

  return poBarcodes.filter((barcode) => !alreadyReceived.has(barcode.toLowerCase()));
}

export function barcodesForReceiveQuantity(
  remainingPoBarcodes: string[],
  quantity: number
): string[] {
  if (quantity <= 0 || remainingPoBarcodes.length === 0) {
    return [];
  }

  return remainingPoBarcodes.slice(0, quantity);
}

export function buildLineReceiptPrefill(
  poLine: PurchaseOrderLine,
  alreadyReceived?: Set<string>
): { quantity: number; barcodesText: string } {
  const quantity = poLine.remainingQuantity;
  const remainingBarcodes = getRemainingPoBarcodes(poLine, alreadyReceived);
  const suggested = barcodesForReceiveQuantity(remainingBarcodes, quantity);

  return {
    quantity,
    barcodesText: formatUnitBarcodes(suggested),
  };
}
