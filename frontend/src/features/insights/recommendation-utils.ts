import type { InsightRecommendation, InsightRecommendationCta } from "@/lib/types";

export function resolveRecommendation(
  item: {
    recommendation?: InsightRecommendation;
    recommendedActionCode: string;
    recommendationParams: Record<string, string>;
    severity: string;
  }
): InsightRecommendation | undefined {
  if (item.recommendation?.actionCode) {
    return item.recommendation;
  }

  if (!item.recommendedActionCode) {
    return undefined;
  }

  return {
    actionCode: item.recommendedActionCode,
    actionType: "review",
    titleKey: item.recommendedActionCode,
    priority: item.severity === "critical" ? 80 : item.severity === "warning" ? 60 : 30,
    params: item.recommendationParams ?? {},
    evidence: item.recommendationParams ?? {},
    actions: [],
  };
}

export function buildNavigateHref(
  route: string,
  payload: Record<string, string>
): string {
  const params = new URLSearchParams();
  Object.entries(payload).forEach(([key, value]) => {
    if (value) {
      params.set(key, value);
    }
  });
  const query = params.toString();
  return query ? `${route}?${query}` : route;
}

export function createTransferPayloadFromCta(
  payload: Record<string, string>
) {
  return {
    sourceWarehouseId: payload.sourceWarehouseId ?? "",
    destinationWarehouseId: payload.destinationWarehouseId ?? "",
    productVariantId: payload.productVariantId ?? "",
    suggestedQuantity: Number(payload.quantity ?? 1),
    sku: payload.sku ?? "",
    sourceWarehouseCode: payload.sourceWarehouseCode ?? "",
    destinationWarehouseCode: payload.destinationWarehouseCode ?? "",
  };
}

export function findCtaById(
  actions: InsightRecommendationCta[] | undefined,
  actionId: string
) {
  return actions?.find((action) => action.id === actionId);
}

export function getRecommendationTitle(
  recommendation: InsightRecommendation | undefined,
  translateTitle: (key: string) => string
): string {
  if (!recommendation?.titleKey) {
    return "";
  }
  try {
    return translateTitle(`recommendation.titles.${recommendation.titleKey}`);
  } catch {
    return recommendation.titleKey;
  }
}

export function getRecommendationDetail(
  recommendation: InsightRecommendation | undefined,
  translateAction: (code: string, params?: Record<string, string>) => string
): string {
  if (!recommendation?.actionCode) {
    return "";
  }
  try {
    return translateAction(recommendation.actionCode, recommendation.params ?? {});
  } catch {
    return recommendation.actionCode;
  }
}

