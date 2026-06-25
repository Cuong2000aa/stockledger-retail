import { WarehouseType } from "./types";
import { formatWarehouseAddress } from "./formatWarehouseAddress";
import { validateLineBarcodes } from "./variantBarcode";
import type { ProductVariant } from "./types";

export const MAX_WAREHOUSE_FULL_ADDRESS_LENGTH = 1000;

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
  form: {
    code: string;
    name: string;
    type: WarehouseType;
    addressLine?: string;
    ward?: string;
    district?: string;
    province?: string;
    postalCode?: string;
  },
  isEditing: boolean
): ValidationIssue[] {
  const issues = collect(
    !isEditing ? required(form.code, "warehouseCodeRequired") : null,
    required(form.name, "warehouseNameRequired")
  );

  if (form.type === WarehouseType.Dc || form.type === WarehouseType.Store) {
    issues.push(
      ...collect(
        required(form.addressLine, "warehouseAddressLineRequired"),
        required(form.ward, "warehouseWardRequired"),
        required(form.district, "warehouseDistrictRequired"),
        required(form.province, "warehouseProvinceRequired")
      )
    );
  }

  const fullAddress = formatWarehouseAddress(form);
  if (fullAddress.length > MAX_WAREHOUSE_FULL_ADDRESS_LENGTH) {
    issues.push({
      key: "warehouseFullAddressTooLong",
      values: { max: MAX_WAREHOUSE_FULL_ADDRESS_LENGTH },
    });
  }

  return issues;
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
  barcodesText?: string;
};

export type InventoryDocumentKind =
  | "stock-in"
  | "stock-out"
  | "adjustment"
  | "transfer"
  | "stock-count";

function appendBarcodeIssues(
  issues: ValidationIssue[],
  variantById: Map<string, ProductVariant> | undefined,
  line: DocumentLineInput,
  lineNo: number,
  quantity: number
) {
  const barcodeIssue = validateLineBarcodes(
    variantById?.get(line.productVariantId),
    quantity,
    line.barcodesText,
    lineNo
  );
  if (barcodeIssue) {
    issues.push(barcodeIssue);
  }
}

export function validateInventoryDocumentForm(input: {
  kind: InventoryDocumentKind;
  warehouseId: string;
  sourceWarehouseId: string;
  destinationWarehouseId: string;
  reason: string;
  lines: DocumentLineInput[];
  hasVariants: boolean;
  variantById?: Map<string, ProductVariant>;
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
      } else {
        appendBarcodeIssues(
          issues,
          input.variantById,
          line,
          lineNo,
          line.adjustmentQuantity
        );
      }
      return;
    }

    if (input.kind === "stock-count") {
      if (line.countedQuantity < 0) {
        issues.push({ key: "lineCountedNonNegative", values: { line: lineNo } });
      } else if (line.countedQuantity > 0) {
        appendBarcodeIssues(
          issues,
          input.variantById,
          line,
          lineNo,
          line.countedQuantity
        );
      }
      return;
    }

    if (!line.quantity || line.quantity <= 0) {
      issues.push({ key: "lineQuantityPositive", values: { line: lineNo } });
    } else {
      appendBarcodeIssues(issues, input.variantById, line, lineNo, line.quantity);
    }
  });

  return issues;
}

export function validateInventoryDocumentDraftLines(input: {
  kind: InventoryDocumentKind;
  lines: DocumentLineInput[];
  hasVariants: boolean;
  variantById?: Map<string, ProductVariant>;
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
      } else {
        appendBarcodeIssues(
          issues,
          input.variantById,
          line,
          lineNo,
          line.adjustmentQuantity
        );
      }
      return;
    }

    if (input.kind === "stock-count") {
      if (line.countedQuantity < 0) {
        issues.push({ key: "lineCountedNonNegative", values: { line: lineNo } });
      } else if (line.countedQuantity > 0) {
        appendBarcodeIssues(
          issues,
          input.variantById,
          line,
          lineNo,
          line.countedQuantity
        );
      }
      return;
    }

    if (!line.quantity || line.quantity <= 0) {
      issues.push({ key: "lineQuantityPositive", values: { line: lineNo } });
    } else {
      appendBarcodeIssues(issues, input.variantById, line, lineNo, line.quantity);
    }
  });

  return issues;
}

export function validatePurchaseOrderForm(input: {
  supplierId: string;
  warehouseId: string;
  lines: Array<{
    productVariantId: string;
    orderedQuantity: number;
    barcodesText?: string;
  }>;
  hasVariants: boolean;
  variantById?: Map<string, ProductVariant>;
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
    const barcodeIssue = validateLineBarcodes(
      input.variantById?.get(line.productVariantId),
      line.orderedQuantity,
      line.barcodesText,
      lineNo
    );
    if (barcodeIssue) {
      issues.push(barcodeIssue);
    }
  });

  return issues;
}

export function validateGoodsReceiptForm(input: {
  lines: Array<{
    productVariantId: string;
    receivedQuantity: number;
    barcodesText?: string;
  }>;
  variantById?: Map<string, ProductVariant>;
}): ValidationIssue[] {
  const issues: ValidationIssue[] = [];
  const hasPositive = input.lines.some((line) => line.receivedQuantity > 0);
  if (!hasPositive) {
    return [{ key: "goodsReceiptQuantityRequired" }];
  }

  input.lines.forEach((line, index) => {
    if (line.receivedQuantity <= 0) {
      return;
    }

    const lineNo = index + 1;
    const barcodeIssue = validateLineBarcodes(
      input.variantById?.get(line.productVariantId),
      line.receivedQuantity,
      line.barcodesText,
      lineNo
    );
    if (barcodeIssue) {
      issues.push(barcodeIssue);
    }
  });

  return issues;
}
