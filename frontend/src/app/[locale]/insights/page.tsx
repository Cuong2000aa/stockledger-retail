"use client";

import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import { fetchWarehouses } from "@/lib/api";
import { formatNumber } from "@/lib/format";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/routing";
import { useMemo, useState } from "react";
import {
  createTransferFromSuggestion,
  fetchDeadStockInsights,
  fetchSalesVelocityInsights,
  fetchTransferSuggestions,
} from "@/features/insights/api";
import { insightQueryKeys } from "@/features/insights/queries";
import type { TransferSuggestion } from "@/features/insights/types";

type RecommendationParams = Record<string, string>;

export default function InsightsPage() {
  const t = useTranslations("insights");
  const tActions = useTranslations("insights.actions");
  const tCommon = useTranslations("common");
  const tStocks = useTranslations("stocks");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { notifyError } = useNotify();
  const [warehouseId, setWarehouseId] = useState<string>("");
  const [daysWithoutOutbound, setDaysWithoutOutbound] = useState(60);
  const [lookbackDays, setLookbackDays] = useState(30);
  const [creatingTransferKey, setCreatingTransferKey] = useState<string | null>(null);

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses", "all-for-insights"],
    queryFn: () => fetchWarehouses(1, 200),
  });

  const activeWarehouseId = warehouseId || undefined;

  const { data: deadStock, isLoading: deadStockLoading } = useQuery({
    queryKey: insightQueryKeys.deadStock(activeWarehouseId, daysWithoutOutbound),
    queryFn: () => fetchDeadStockInsights(activeWarehouseId, daysWithoutOutbound, 1, 20),
  });

  const { data: salesVelocity, isLoading: salesVelocityLoading } = useQuery({
    queryKey: insightQueryKeys.salesVelocity(activeWarehouseId, lookbackDays),
    queryFn: () => fetchSalesVelocityInsights(activeWarehouseId, lookbackDays, 20),
  });

  const { data: transferSuggestions, isLoading: transferSuggestionsLoading } = useQuery({
    queryKey: insightQueryKeys.transferSuggestions(undefined, activeWarehouseId, lookbackDays),
    queryFn: () => fetchTransferSuggestions(undefined, activeWarehouseId, lookbackDays, 14, 7, 20),
  });

  const createTransferMutation = useMutation({
    mutationFn: createTransferFromSuggestion,
    onSuccess: (doc) => {
      setCreatingTransferKey(null);
      void queryClient.invalidateQueries({ queryKey: ["inventory-documents"] });
      router.push(`/inventory-documents/${doc.id}`);
    },
    onError: (error) => {
      setCreatingTransferKey(null);
      notifyError(error);
    },
  });

  const warehouseOptions = useMemo(
    () => warehouses?.items ?? [],
    [warehouses?.items]
  );

  const handleCreateTransfer = (item: TransferSuggestion, rowKey: string) => {
    setCreatingTransferKey(rowKey);
    createTransferMutation.mutate(item);
  };

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      <div className="card mb-6 grid gap-4 p-4 md:grid-cols-3">
        <label className="text-sm text-slate-600">
          <span className="mb-1 block">{t("filters.warehouse")}</span>
          <select
            className="input"
            value={warehouseId}
            onChange={(e) => setWarehouseId(e.target.value)}
          >
            <option value="">{t("filters.allWarehouses")}</option>
            {warehouseOptions.map((warehouse) => (
              <option key={warehouse.id} value={warehouse.id}>
                {warehouse.code} - {warehouse.name}
              </option>
            ))}
          </select>
        </label>

        <label className="text-sm text-slate-600">
          <span className="mb-1 block">{t("filters.deadStockDays")}</span>
          <input
            type="number"
            min={1}
            className="input"
            value={daysWithoutOutbound}
            onChange={(e) => setDaysWithoutOutbound(Number(e.target.value) || 1)}
          />
        </label>

        <label className="text-sm text-slate-600">
          <span className="mb-1 block">{t("filters.lookbackDays")}</span>
          <input
            type="number"
            min={1}
            className="input"
            value={lookbackDays}
            onChange={(e) => setLookbackDays(Number(e.target.value) || 1)}
          />
        </label>
      </div>

      <div className="mb-6 grid gap-6 xl:grid-cols-1">
        <div className="card overflow-hidden">
          <div className="border-b border-slate-200 px-4 py-3">
            <h2 className="font-semibold text-slate-900">{t("deadStock.title")}</h2>
            <p className="mt-1 text-sm text-slate-500">{t("deadStock.subtitle")}</p>
          </div>
          <div className="table-wrap max-h-[28rem] overflow-y-auto">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("deadStock.days")}</th>
                  <th>{tStocks("onHand")}</th>
                  <th>{t("deadStock.costValue")}</th>
                  <th>{t("recommendation")}</th>
                </tr>
              </thead>
              <tbody>
                {deadStockLoading ? (
                  <LoadingRow colSpan={6} label={tCommon("loading")} />
                ) : deadStock?.length ? (
                  deadStock.map((item) => (
                    <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                      <td className="font-mono text-xs">{item.sku}</td>
                      <td>{item.warehouseCode}</td>
                      <td className={severityClass(item.severity)}>
                        {formatNumber(item.daysWithoutOutbound, locale)}
                      </td>
                      <td>{formatNumber(item.quantityOnHand, locale)}</td>
                      <td>{formatNumber(item.estimatedCostValue ?? 0, locale)}</td>
                      <td className="max-w-xs text-sm text-slate-600">
                        <RecommendationText
                          actionCode={item.recommendedActionCode}
                          params={item.recommendationParams}
                          translate={tActions}
                        />
                      </td>
                    </tr>
                  ))
                ) : (
                  <EmptyRow colSpan={6} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card overflow-hidden">
          <div className="border-b border-slate-200 px-4 py-3">
            <h2 className="font-semibold text-slate-900">{t("salesVelocity.title")}</h2>
            <p className="mt-1 text-sm text-slate-500">{t("salesVelocity.subtitle")}</p>
          </div>
          <div className="table-wrap max-h-[28rem] overflow-y-auto">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{tStocks("warehouse")}</th>
                  <th>{t("salesVelocity.outbound")}</th>
                  <th>{t("salesVelocity.avgDaily")}</th>
                  <th>{t("salesVelocity.coverDays")}</th>
                  <th>{t("recommendation")}</th>
                </tr>
              </thead>
              <tbody>
                {salesVelocityLoading ? (
                  <LoadingRow colSpan={6} label={tCommon("loading")} />
                ) : salesVelocity?.length ? (
                  salesVelocity.map((item) => (
                    <tr key={`${item.productVariantId}-${item.warehouseId}`}>
                      <td className="font-mono text-xs">{item.sku}</td>
                      <td>{item.warehouseCode}</td>
                      <td>{formatNumber(item.outboundQuantity, locale)}</td>
                      <td>{formatNumber(item.averageDailyOutbound, locale)}</td>
                      <td className={severityClass(item.severity)}>
                        {item.estimatedDaysOfCover != null
                          ? formatNumber(item.estimatedDaysOfCover, locale)
                          : t("salesVelocity.noDemand")}
                      </td>
                      <td className="max-w-xs text-sm text-slate-600">
                        <RecommendationText
                          actionCode={item.recommendedActionCode}
                          params={item.recommendationParams}
                          translate={tActions}
                        />
                      </td>
                    </tr>
                  ))
                ) : (
                  <EmptyRow colSpan={6} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card overflow-hidden">
          <div className="border-b border-slate-200 px-4 py-3">
            <h2 className="font-semibold text-slate-900">{t("transfer.title")}</h2>
            <p className="mt-1 text-sm text-slate-500">{t("transfer.subtitle")}</p>
          </div>
          <div className="table-wrap max-h-[28rem] overflow-y-auto">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{tStocks("sku")}</th>
                  <th>{t("transfer.from")}</th>
                  <th>{t("transfer.to")}</th>
                  <th>{t("transfer.qty")}</th>
                  <th>{t("transfer.coverDays")}</th>
                  <th>{t("recommendation")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {transferSuggestionsLoading ? (
                  <LoadingRow colSpan={7} label={tCommon("loading")} />
                ) : transferSuggestions?.length ? (
                  transferSuggestions.map((item, index) => {
                    const rowKey = `${item.productVariantId}-${item.sourceWarehouseId}-${item.destinationWarehouseId}-${index}`;
                    const isCreating = creatingTransferKey === rowKey;

                    return (
                      <tr key={rowKey}>
                        <td className="font-mono text-xs">{item.sku}</td>
                        <td>{item.sourceWarehouseCode}</td>
                        <td>{item.destinationWarehouseCode}</td>
                        <td>{formatNumber(item.suggestedQuantity, locale)}</td>
                        <td className={severityClass(item.severity)}>
                          {item.destinationDaysOfCover != null
                            ? formatNumber(item.destinationDaysOfCover, locale)
                            : tCommon("noData")}
                        </td>
                        <td className="max-w-xs text-sm text-slate-600">
                          <RecommendationText
                            actionCode={item.recommendedActionCode}
                            params={item.recommendationParams}
                            translate={tActions}
                          />
                        </td>
                        <td className="whitespace-nowrap">
                          <button
                            type="button"
                            className="btn-secondary text-xs"
                            disabled={isCreating || createTransferMutation.isPending}
                            onClick={() => handleCreateTransfer(item, rowKey)}
                          >
                            {isCreating ? t("transfer.creating") : t("transfer.createTransfer")}
                          </button>
                        </td>
                      </tr>
                    );
                  })
                ) : (
                  <EmptyRow colSpan={7} label={tCommon("noData")} />
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}

function RecommendationText({
  actionCode,
  params,
  translate,
}: {
  actionCode?: string;
  params?: RecommendationParams;
  translate: ReturnType<typeof useTranslations<"insights.actions">>;
}) {
  if (!actionCode) {
    return <span>—</span>;
  }

  try {
    return <span>{translate(actionCode as never, params ?? {})}</span>;
  } catch {
    return <span className="font-mono text-xs">{actionCode}</span>;
  }
}

function LoadingRow({ colSpan, label }: { colSpan: number; label: string }) {
  return (
    <tr>
      <td colSpan={colSpan} className="text-slate-500">
        {label}
      </td>
    </tr>
  );
}

function EmptyRow({ colSpan, label }: { colSpan: number; label: string }) {
  return (
    <tr>
      <td colSpan={colSpan} className="text-slate-500">
        {label}
      </td>
    </tr>
  );
}

function severityClass(severity: string) {
  if (severity === "critical") {
    return "text-red-700";
  }

  if (severity === "warning") {
    return "text-amber-700";
  }

  return "text-slate-700";
}
