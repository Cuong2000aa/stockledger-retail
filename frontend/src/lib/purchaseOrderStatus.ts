import { PurchaseOrderStatus } from "./types";

type PurchaseOrderT = (key: string) => string;

export function purchaseOrderStatusLabel(
  status: PurchaseOrderStatus,
  t: PurchaseOrderT
): string {
  switch (status) {
    case PurchaseOrderStatus.Draft:
      return t("statusDraft");
    case PurchaseOrderStatus.Submitted:
      return t("statusSubmitted");
    case PurchaseOrderStatus.PartiallyReceived:
      return t("statusPartiallyReceived");
    case PurchaseOrderStatus.Received:
      return t("statusReceived");
    case PurchaseOrderStatus.Cancelled:
      return t("statusCancelled");
    case PurchaseOrderStatus.PendingApproval:
      return t("statusPendingApproval");
    default:
      return String(status);
  }
}
