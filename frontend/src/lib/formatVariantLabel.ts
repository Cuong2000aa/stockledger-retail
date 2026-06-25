import type { ProductVariant } from "./types";

export function formatVariantOptionLabel(variant: ProductVariant): string {
  if (variant.isBarcode) {
    return `${variant.sku} · Barcode`;
  }
  return variant.sku;
}
