export type ValidationIssue = {
  key: string;
  values?: Record<string, string | number>;
};

function required(
  value: string | undefined | null,
  key: string
): ValidationIssue | null {
  if (!value?.trim()) {
    return { key };
  }
  return null;
}

function collect(...issues: Array<ValidationIssue | null>): ValidationIssue[] {
  return issues.filter((issue): issue is ValidationIssue => issue !== null);
}

export function validateProductForm(
  form: { productCode: string; name: string },
  isEditing: boolean
): ValidationIssue[] {
  return collect(
    !isEditing ? required(form.productCode, "productCodeRequired") : null,
    required(form.name, "productNameRequired")
  );
}

export function validateWarehouseForm(
  form: { code: string; name: string },
  isEditing: boolean
): ValidationIssue[] {
  return collect(
    !isEditing ? required(form.code, "warehouseCodeRequired") : null,
    required(form.name, "warehouseNameRequired")
  );
}

export function validateSupplierForm(
  form: { code: string; name: string },
  isEditing: boolean
): ValidationIssue[] {
  return collect(
    !isEditing ? required(form.code, "supplierCodeRequired") : null,
    required(form.name, "supplierNameRequired")
  );
}

export function validateProductVariantForm(form: {
  productId: string;
  sku: string;
}): ValidationIssue[] {
  return collect(
    required(form.productId, "productRequired"),
    required(form.sku, "skuRequired")
  );
}

export type DocumentLineInput = {
  productVariantId: string;
  quantity: number;
  adjustmentQuantity: number;
  countedQuantity: number;
};

export type InventoryDocumentKind =
  | "stock-in"
  | "stock-out"
  | "adjustment"
  | "transfer"
  | "stock-count";

export function validateInventoryDocumentForm(input: {
  kind: InventoryDocumentKind;
  warehouseId: string;
  sourceWarehouseId: string;
  destinationWarehouseId: string;
  reason: string;
  lines: DocumentLineInput[];
  hasVariants: boolean;
}): ValidationIssue[] {
  const issues: ValidationIssue[] = [];

  if (!input.hasVariants) {
    issues.push({ key: "noSkuAvailable" });
  }

  if (input.kind === "transfer") {
    if (!input.sourceWarehouseId) {
      issues.push({ key: "sourceWarehouseRequired" });
    }
    if (!input.destinationWarehouseId) {
      issues.push({ key: "destinationWarehouseRequired" });
    }
    if (
      input.sourceWarehouseId &&
      input.destinationWarehouseId &&
      input.sourceWarehouseId === input.destinationWarehouseId
    ) {
      issues.push({ key: "transferSameWarehouse" });
    }
  } else if (!input.warehouseId) {
    issues.push({ key: "warehouseRequired" });
  }

  if (input.kind === "adjustment" && !input.reason.trim()) {
    issues.push({ key: "adjustmentReasonRequired" });
  }

  if (input.lines.length === 0) {
    issues.push({ key: "documentNeedsLine" });
    return issues;
  }

  input.lines.forEach((line, index) => {
    const lineNo = index + 1;

    if (!line.productVariantId) {
      issues.push({ key: "lineSkuRequired", values: { line: lineNo } });
    }

    if (input.kind === "adjustment") {
      if (!line.adjustmentQuantity || line.adjustmentQuantity === 0) {
        issues.push({ key: "lineAdjustmentNonZero", values: { line: lineNo } });
      }
      return;
    }

    if (input.kind === "stock-count") {
      if (line.countedQuantity < 0) {
        issues.push({ key: "lineCountedNonNegative", values: { line: lineNo } });
      }
      return;
    }

    if (!line.quantity || line.quantity <= 0) {
      issues.push({ key: "lineQuantityPositive", values: { line: lineNo } });
    }
  });

  return issues;
}

export function validateInventoryDocumentDraftLines(input: {
  kind: InventoryDocumentKind;
  lines: DocumentLineInput[];
  hasVariants: boolean;
}): ValidationIssue[] {
  const issues: ValidationIssue[] = [];

  if (!input.hasVariants) {
    issues.push({ key: "noSkuAvailable" });
  }

  if (input.lines.length === 0) {
    issues.push({ key: "documentNeedsLine" });
    return issues;
  }

  input.lines.forEach((line, index) => {
    const lineNo = index + 1;

    if (!line.productVariantId) {
      issues.push({ key: "lineSkuRequired", values: { line: lineNo } });
    }

    if (input.kind === "adjustment") {
      if (!line.adjustmentQuantity || line.adjustmentQuantity === 0) {
        issues.push({ key: "lineAdjustmentNonZero", values: { line: lineNo } });
      }
      return;
    }

    if (input.kind === "stock-count") {
      if (line.countedQuantity < 0) {
        issues.push({ key: "lineCountedNonNegative", values: { line: lineNo } });
      }
      return;
    }

    if (!line.quantity || line.quantity <= 0) {
      issues.push({ key: "lineQuantityPositive", values: { line: lineNo } });
    }
  });

  return issues;
}

export function validatePurchaseOrderForm(input: {
  supplierId: string;
  warehouseId: string;
  lines: Array<{ productVariantId: string; orderedQuantity: number }>;
  hasVariants: boolean;
}): ValidationIssue[] {
  const issues: ValidationIssue[] = [];

  if (!input.supplierId) {
    issues.push({ key: "supplierRequired" });
  }
  if (!input.warehouseId) {
    issues.push({ key: "warehouseRequired" });
  }
  if (!input.hasVariants) {
    issues.push({ key: "noSkuAvailable" });
  }
  if (input.lines.length === 0) {
    issues.push({ key: "documentNeedsLine" });
    return issues;
  }

  input.lines.forEach((line, index) => {
    const lineNo = index + 1;
    if (!line.productVariantId) {
      issues.push({ key: "lineSkuRequired", values: { line: lineNo } });
    }
    if (!line.orderedQuantity || line.orderedQuantity <= 0) {
      issues.push({ key: "lineQuantityPositive", values: { line: lineNo } });
    }
  });

  return issues;
}

export function validateGoodsReceiptForm(input: {
  lines: Array<{ receivedQuantity: number }>;
}): ValidationIssue[] {
  const hasPositive = input.lines.some((line) => line.receivedQuantity > 0);
  if (!hasPositive) {
    return [{ key: "goodsReceiptQuantityRequired" }];
  }
  return [];
}
