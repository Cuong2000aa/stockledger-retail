"use client";

import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { fetchStockReservations, releaseStockReservation } from "@/features/reports/api";
import { formatDate, formatNumber } from "@/lib/format";
import { StockReservationStatus } from "@/lib/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { Clock3 } from "lucide-react";
import { useState } from "react";
import { useNotify } from "@/hooks/useNotify";

export default function StockReservationsPage() {
  const t = useTranslations("stockReservations");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const qc = useQueryClient();
  const { notifyError, confirm } = useNotify();
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ["stock-reservations", page],
    queryFn: () => fetchStockReservations(undefined, StockReservationStatus.Active, page, 20),
    refetchInterval: 15_000,
  });

  const releaseMutation = useMutation({
    mutationFn: releaseStockReservation,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["stock-reservations"] }),
    onError: notifyError,
  });

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      <DataTableCard title={t("title")} icon={Clock3}>
        {isLoading ? (
          <p className="p-4 text-slate-500">{tCommon("loading")}</p>
        ) : !data?.items.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{t("reservationNo")}</th>
                  <th>{t("warehouse")}</th>
                  <th>{t("reference")}</th>
                  <th>{t("qty")}</th>
                  <th>{t("expires")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.items.map((r) => (
                  <tr key={r.id}>
                    <td><CodePill>{r.reservationNo}</CodePill></td>
                    <td>{r.warehouseCode}</td>
                    <td className="text-xs">{r.referenceKey}</td>
                    <td>{formatNumber(r.totalQuantity, locale)}</td>
                    <td>{formatDate(r.expiresAt, locale)}</td>
                    <td className="text-right">
                      <button
                        type="button"
                        className="btn-ghost text-sm text-red-600"
                        onClick={async () => {
                          if (await confirm(t("releaseConfirm"))) {
                            releaseMutation.mutate(r.id);
                          }
                        }}
                      >
                        {t("release")}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {data && data.totalCount > data.pageSize && (
          <Pagination page={page} pageSize={data.pageSize} totalCount={data.totalCount} onChange={setPage} />
        )}
      </DataTableCard>
    </div>
  );
}
