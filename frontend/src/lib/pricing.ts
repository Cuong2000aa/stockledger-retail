export function roundCurrency(value: number): number {
  return Math.round(value * 10000) / 10000;
}

export function parsePriceField(value: string): number | undefined {
  const trimmed = value.trim();
  if (trimmed === "") return undefined;
  const parsed = Number(trimmed);
  return Number.isNaN(parsed) ? undefined : parsed;
}

export function calcPriceAfterVat(priceBeforeVat: number, vatRate: number): number {
  return roundCurrency(priceBeforeVat * (1 + vatRate / 100));
}

export function calcPriceBeforeVat(priceAfterVat: number, vatRate: number): number {
  const divisor = 1 + vatRate / 100;
  return divisor === 0 ? priceAfterVat : roundCurrency(priceAfterVat / divisor);
}

export function recalcPriceAfterVat(form: {
  priceBeforeVat: string;
  vatRate: string;
}): string {
  const before = parsePriceField(form.priceBeforeVat);
  const vat = parsePriceField(form.vatRate) ?? 0;
  if (before == null) return "";
  return String(calcPriceAfterVat(before, vat));
}

export function recalcPriceBeforeVat(form: {
  priceAfterVat: string;
  vatRate: string;
}): string {
  const after = parsePriceField(form.priceAfterVat);
  const vat = parsePriceField(form.vatRate) ?? 0;
  if (after == null) return "";
  return String(calcPriceBeforeVat(after, vat));
}

export function recalcFromVatChange(form: {
  priceBeforeVat: string;
  priceAfterVat: string;
  vatRate: string;
}): { priceBeforeVat: string; priceAfterVat: string } {
  const before = parsePriceField(form.priceBeforeVat);
  const after = parsePriceField(form.priceAfterVat);
  const vat = parsePriceField(form.vatRate) ?? 0;

  if (before != null) {
    return {
      priceBeforeVat: form.priceBeforeVat,
      priceAfterVat: String(calcPriceAfterVat(before, vat)),
    };
  }

  if (after != null) {
    return {
      priceBeforeVat: String(calcPriceBeforeVat(after, vat)),
      priceAfterVat: form.priceAfterVat,
    };
  }

  return {
    priceBeforeVat: form.priceBeforeVat,
    priceAfterVat: form.priceAfterVat,
  };
}
