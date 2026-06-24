import type { useTranslations } from "next-intl";

type OperationsTranslator = ReturnType<typeof useTranslations<"operations">>;

export function getJobName(
  t: OperationsTranslator,
  jobKey: string,
  fallback: string
) {
  const key = `jobs.${jobKey}.name` as const;
  return t.has(key) ? t(key) : fallback;
}

export function getJobDescription(
  t: OperationsTranslator,
  jobKey: string,
  fallback?: string
) {
  const key = `jobs.${jobKey}.description` as const;
  return t.has(key) ? t(key) : (fallback ?? "");
}

export function formatTrigger(t: OperationsTranslator, triggeredBy: string) {
  if (triggeredBy === "scheduled") {
    return t("triggers.scheduled");
  }
  if (triggeredBy === "manual") {
    return t("triggers.manual");
  }
  if (triggeredBy.startsWith("manual:")) {
    return t("triggers.manualBy", { email: triggeredBy.slice("manual:".length) });
  }
  return triggeredBy;
}
