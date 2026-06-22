type ApiErrorTranslator = (
  key: string,
  values?: Record<string, string | number>
) => string;

const API_ERROR_RULES: Array<{
  pattern: RegExp;
  key: string;
  mapValues?: (match: RegExpMatchArray) => Record<string, string | number>;
}> = [
  {
    pattern: /Product variant '([^']+)' was not found/i,
    key: "productVariantNotFound",
  },
  {
    pattern: /Warehouse '([^']+)' was not found/i,
    key: "warehouseNotFound",
  },
  {
    pattern: /SKU '([^']+)' was not found/i,
    key: "skuNotFound",
    mapValues: (match) => ({ sku: match[1] }),
  },
  {
    pattern: /Insufficient (?:available )?stock/i,
    key: "insufficientStock",
  },
  {
    pattern: /Line quantity must be greater than zero/i,
    key: "lineQuantityPositive",
  },
  {
    pattern: /Document must contain at least one line/i,
    key: "documentNeedsLine",
  },
  {
    pattern: /Adjustment reason is required/i,
    key: "adjustmentReasonRequired",
  },
  {
    pattern: /Adjustment quantity cannot be zero/i,
    key: "adjustmentQuantityNonZero",
  },
  {
    pattern: /Counted quantity cannot be negative/i,
    key: "countedQuantityNonNegative",
  },
  {
    pattern: /Source and destination warehouse cannot be the same/i,
    key: "transferSameWarehouse",
  },
  {
    pattern: /Only draft documents can be updated/i,
    key: "onlyDraftEditable",
  },
  {
    pattern: /Document is already approved/i,
    key: "documentAlreadyApproved",
  },
  {
    pattern: /Cancelled documents cannot be approved/i,
    key: "cancelledCannotApprove",
  },
  {
    pattern: /Only draft documents can be cancelled/i,
    key: "onlyDraftCancellable",
  },
  {
    pattern: /Inventory quantity cannot become negative/i,
    key: "inventoryNegative",
  },
  {
    pattern: /Either cartSessionId or orderReference is required/i,
    key: "reservationReferenceRequired",
  },
];

export function formatApiErrorMessage(
  message: string,
  t: ApiErrorTranslator
): string {
  const normalized = message.trim();
  if (!normalized) {
    return t("unknown");
  }

  for (const rule of API_ERROR_RULES) {
    const match = normalized.match(rule.pattern);
    if (match) {
      return t(rule.key, rule.mapValues?.(match));
    }
  }

  if (normalized === "Request failed" || normalized.includes("status code")) {
    return t("requestFailed");
  }

  return normalized;
}
