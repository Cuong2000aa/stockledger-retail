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
    pattern: /Address line is required for DC and store warehouses/i,
    key: "warehouseAddressLineRequired",
  },
  {
    pattern: /Ward is required for DC and store warehouses/i,
    key: "warehouseWardRequired",
  },
  {
    pattern: /District is required for DC and store warehouses/i,
    key: "warehouseDistrictRequired",
  },
  {
    pattern: /Province is required for DC and store warehouses/i,
    key: "warehouseProvinceRequired",
  },
  {
    pattern: /Full address cannot exceed (\d+) characters/i,
    key: "warehouseFullAddressTooLong",
    mapValues: (match) => ({ max: Number(match[1]) }),
  },
  {
    pattern: /Either cartSessionId or orderReference is required/i,
    key: "reservationReferenceRequired",
  },
  {
    pattern: /^AUTH_CANNOT_APPROVE_DOCUMENT$/i,
    key: "cannotApproveDocument",
  },
  {
    pattern: /^AUTH_CANNOT_APPROVE_GOODS_RECEIPT$/i,
    key: "cannotApproveGoodsReceipt",
  },
  {
    pattern: /^AUTH_CANNOT_MANAGE_DOCUMENT$/i,
    key: "cannotManageDocument",
  },
  {
    pattern: /^AUTH_CANNOT_RECEIVE_TRANSFER$/i,
    key: "cannotReceiveTransfer",
  },
  {
    pattern: /^WORKFLOW_HIGH_VALUE_SUBMIT_REQUIRED$/i,
    key: "highValueSubmitRequired",
  },
  {
    pattern: /^AUTH_MISSING_PERMISSION:(.+)$/i,
    key: "missingPermission",
    mapValues: (match) => ({ permission: match[1] }),
  },
  {
    pattern: /You are not allowed to approve this document/i,
    key: "cannotApproveDocument",
  },
  {
    pattern: /High-value documents must be submitted for approval before final approval/i,
    key: "highValueSubmitRequired",
  },
  {
    pattern: /Missing permission '([^']+)'/i,
    key: "missingPermission",
    mapValues: (match) => ({ permission: match[1] }),
  },
  {
    pattern: /You can only manage your own documents or team members/i,
    key: "cannotManageDocument",
  },
  {
    pattern: /You are not allowed to receive this transfer/i,
    key: "cannotReceiveTransfer",
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
