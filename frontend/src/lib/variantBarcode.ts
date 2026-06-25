import type { ProductVariant } from "./types";
import type { ValidationIssue } from "./validation";
import { parseUnitBarcodes } from "./unitBarcode";

export function variantRequiresBarcode(
  variant: ProductVariant | undefined
): boolean {
  return !!variant?.isBarcode;
}

export function validateLineBarcodes(
  variant: ProductVariant | undefined,
  quantity: number,
  barcodesText: string | undefined,
  lineNo: number
): ValidationIssue | null {
  if (!variantRequiresBarcode(variant)) {
    return null;
  }

  const expected = Math.abs(quantity);
  if (!Number.isInteger(expected)) {
    return { key: "lineBarcodeWholeQuantity", values: { line: lineNo } };
  }

  const barcodes = parseUnitBarcodes(barcodesText ?? "");
  if (barcodes.length !== expected) {
    return {
      key: "lineBarcodeCountMismatch",
      values: { line: lineNo, expected, actual: barcodes.length },
    };
  }

  return null;
}

export function validateProductVariantBarcodeSettings(): ValidationIssue | null {
  return null;
}
