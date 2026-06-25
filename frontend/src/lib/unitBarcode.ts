/** Delimiter chuẩn khi nhập/hiển thị barcode. */
export const UNIT_BARCODE_SEPARATOR = ", ";

export function parseUnitBarcodes(text: string): string[] {
  const seen = new Set<string>();
  const result: string[] = [];

  const normalized = text.replace(/[\r\n;\t]+/g, ",");

  for (const raw of normalized.split(",")) {
    const trimmed = raw.trim();
    if (!trimmed) {
      continue;
    }

    const key = trimmed.toLowerCase();
    if (!seen.has(key)) {
      seen.add(key);
      result.push(trimmed);
    }
  }

  return result;
}

export function formatUnitBarcodes(barcodes: string[]): string {
  return barcodes.join(UNIT_BARCODE_SEPARATOR);
}

/** Chuẩn hóa chuỗi nhập về dạng phân tách bằng dấu phẩy. */
export function normalizeUnitBarcodesText(text: string): string {
  return formatUnitBarcodes(parseUnitBarcodes(text));
}
