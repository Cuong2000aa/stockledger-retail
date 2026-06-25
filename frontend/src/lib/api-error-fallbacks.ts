/** Dự phòng khi next-intl chưa load key (cache dev) — tránh hiện raw key trên UI. */
export const API_ERROR_FALLBACKS: Record<string, Record<string, string>> = {
  vi: {
    unknown: "Đã xảy ra lỗi không xác định.",
    requestFailed: "Không gọi được API. Kiểm tra backend đang chạy.",
    cannotApproveDocument:
      "Bạn không có quyền duyệt phiếu này. Cần quyền duyệt phiếu kho hoặc là trưởng nhóm của người tạo phiếu.",
    cannotApproveGoodsReceipt: "Bạn không có quyền duyệt phiếu nhận hàng này.",
    cannotManageDocument:
      "Bạn chỉ được thao tác phiếu do mình tạo, hoặc phiếu của thành viên trong nhóm nếu là trưởng nhóm.",
    cannotReceiveTransfer: "Bạn không có quyền nhận phiếu chuyển kho này.",
    highValueSubmitRequired:
      "Phiếu có giá trị cao — cần gửi duyệt trước khi duyệt chính thức.",
    missingPermission: "Thiếu quyền: {permission}.",
    insufficientStock: "Không đủ tồn khả dụng để thực hiện.",
    documentAlreadyApproved: "Phiếu đã được duyệt.",
    cancelledCannotApprove: "Phiếu đã hủy, không thể duyệt.",
  },
  en: {
    unknown: "An unknown error occurred.",
    requestFailed: "Could not reach the API.",
    cannotApproveDocument:
      "You are not allowed to approve this document. Approve permission or team-leader role is required.",
    cannotApproveGoodsReceipt: "You are not allowed to approve this goods receipt.",
    cannotManageDocument:
      "You can only manage your own documents or team members' documents as team leader.",
    cannotReceiveTransfer: "You are not allowed to receive this transfer.",
    highValueSubmitRequired:
      "This document exceeds the value threshold — submit it for approval before final approval.",
    missingPermission: "Missing permission: {permission}.",
    insufficientStock: "Insufficient available stock for this action.",
    documentAlreadyApproved: "This document is already approved.",
    cancelledCannotApprove: "Cancelled documents cannot be approved.",
  },
};

export function getApiErrorFallback(
  locale: string,
  key: string,
  values?: Record<string, string | number>
): string {
  const lang = locale.startsWith("vi") ? "vi" : "en";
  let text = API_ERROR_FALLBACKS[lang][key] ?? API_ERROR_FALLBACKS.en[key] ?? key;
  if (values) {
    for (const [k, v] of Object.entries(values)) {
      text = text.replace(`{${k}}`, String(v));
    }
  }
  return text;
}
