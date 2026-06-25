import { getApiErrorFallback } from "@/lib/api-error-fallbacks";

type FallbackInfo = {
  error: unknown;
  key: string;
  namespace?: string;
  locale: string;
};

export function getIntlMessageFallback({ namespace, key, locale }: FallbackInfo) {
  if (namespace === "apiErrors") {
    return getApiErrorFallback(locale, key);
  }
  return namespace ? `${namespace}.${key}` : key;
}
