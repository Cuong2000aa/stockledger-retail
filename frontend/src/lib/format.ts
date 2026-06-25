export function formatDate(value: string | undefined, locale: string) {
  if (!value) return "—";
  return new Date(value).toLocaleString(locale === "vi" ? "vi-VN" : "en-US");
}

/** Ngày không kèm giờ — dùng cho HSD, kỳ báo cáo. */
export function formatDateOnly(value: string | undefined, locale: string) {
  if (!value) return "—";
  return new Date(value).toLocaleDateString(locale === "vi" ? "vi-VN" : "en-US");
}

export function formatNumber(value: number, locale: string) {
  return value.toLocaleString(locale === "vi" ? "vi-VN" : "en-US");
}
