import { StockTransaction, StockTransactionType } from "@/lib/types";

export type StockTransactionDisplayRow = {
  rowKey: string;
  transactionId: string;
  transactionNo: string;
  documentId: string;
  documentNo: string;
  sourceSystem?: string;
  sku: string;
  sourceWarehouse: string;
  destinationWarehouse: string;
  transactionType: StockTransactionType;
  quantityDelta: number;
  barcode?: string;
  createdBy?: string;
  transactionDate: string;
  isSplitLine: boolean;
};

export function isInboundTransaction(type: StockTransactionType) {
  return [
    StockTransactionType.In,
    StockTransactionType.TransferIn,
    StockTransactionType.AdjustmentIn,
    StockTransactionType.CountAdjustmentIn,
  ].includes(type);
}

/** Kho xuất / kho nhận theo chiều giao dịch. */
export function resolveTransactionWarehouses(tx: StockTransaction) {
  const warehouseCode = tx.warehouseCode?.trim();
  const counterpartWarehouseCode = tx.counterpartWarehouseCode?.trim();

  if (isInboundTransaction(tx.transactionType)) {
    const destinationWarehouse = warehouseCode || counterpartWarehouseCode || "—";
    return {
      sourceWarehouse: counterpartWarehouseCode || destinationWarehouse,
      destinationWarehouse,
    };
  }

  const sourceWarehouse = warehouseCode || counterpartWarehouseCode || "—";
  return {
    sourceWarehouse,
    destinationWarehouse: counterpartWarehouseCode || sourceWarehouse,
  };
}

/**
 * Tách dòng theo IMEI: mỗi barcode = 1 dòng SL ±1.
 * Không có barcode → một dòng với full SL (SKU không quản lý IMEI).
 */
export function expandStockTransactionRows(
  tx: StockTransaction
): StockTransactionDisplayRow[] {
  const { sourceWarehouse, destinationWarehouse } = resolveTransactionWarehouses(tx);
  const barcodes = (tx.barcodes ?? []).map((b) => b.trim()).filter(Boolean);
  const sign = tx.quantityDelta >= 0 ? 1 : -1;
  const absQty = Math.abs(tx.quantityDelta);
  const shouldSplitByBarcode = !!tx.isBarcode && barcodes.length > 0;

  const base = {
    transactionId: tx.id,
    transactionNo: tx.transactionNo,
    documentId: tx.documentId,
    documentNo: tx.documentNo,
    sourceSystem: tx.sourceSystem,
    sku: tx.sku,
    sourceWarehouse,
    destinationWarehouse,
    transactionType: tx.transactionType,
    createdBy: tx.createdBy,
    transactionDate: tx.transactionDate,
  };

  if (!shouldSplitByBarcode) {
    return [
      {
        ...base,
        rowKey: tx.id,
        quantityDelta: tx.quantityDelta,
        isSplitLine: false,
      },
    ];
  }

  const rows: StockTransactionDisplayRow[] = barcodes.map((barcode) => ({
    ...base,
    rowKey: `${tx.id}:${barcode}`,
    quantityDelta: sign * 1,
    barcode,
    isSplitLine: true,
  }));

  const remainder = absQty - barcodes.length;
  if (remainder > 0.0001) {
    rows.push({
      ...base,
      rowKey: `${tx.id}:bulk`,
      quantityDelta: sign * remainder,
      isSplitLine: true,
    });
  }

  return rows;
}

export function expandStockTransactionList(
  items: StockTransaction[]
): StockTransactionDisplayRow[] {
  return items.flatMap(expandStockTransactionRows);
}
