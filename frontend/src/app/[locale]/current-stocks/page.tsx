"use client";

import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { fetchCurrentStocks, fetchWarehouses } from "@/lib/api";
import { formatDate, formatNumber } from "@/lib/format";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

export default function CurrentStocksPage() {
  const t = useTranslations("stocks");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [warehouseId, setWarehouseId] = useState("");

  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-all"],
    queryFn: () => fetchWarehouses(1, 100),
  });

  const { data, isLoading } = useQuery({
    queryKey: ["current-stocks", page, warehouseId],
    queryFn: () =>
      fetchCurrentStocks(warehouseId || undefined, undefined, page),
  });

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />

      <div className="mb-4">
        <select
          className="input max-w-xs"
          value={warehouseId}
          onChange={(e) => {
            setWarehouseId(e.target.value);
            setPage(1);
          }}
        >
          <option value="">{t("warehouse")} — All</option>
          {warehouses?.items.map((w) => (
            <option key={w.id} value={w.id}>
              {w.code} — {w.name}
            </option>
          ))}
        </select>
      </div>

      <div className="card">
        {isLoading ? (
          <p className="p-6 text-slate-500">{tCommon("loading")}</p>
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>{t("sku")}</th>
                    <th>{t("warehouse")}</th>
                    <th>{t("onHand")}</th>
                    <th>{t("reserved")}</th>
                    <th>{t("available")}</th>
                    <th>{t("lastUpdated")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.length === 0 && (
                    <tr>
                      <td colSpan={6} className="text-center text-slate-400">
                        {tCommon("noData")}
                      </td>
                    </tr>
                  )}
                  {data?.items.map((s) => (
                    <tr key={s.id}>
                      <td className="font-mono text-xs">{s.sku}</td>
                      <td>
                        {s.warehouseCode} — {s.warehouseName}
                      </td>
                      <td>{formatNumber(s.quantityOnHand, locale)}</td>
                      <td>{formatNumber(s.quantityReserved, locale)}</td>
                      <td className="font-medium text-green-700">
                        {formatNumber(s.quantityAvailable, locale)}
                      </td>
                      <td className="text-xs text-slate-500">
                        {formatDate(s.lastUpdatedAt, locale)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {data && (
              <Pagination
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                onChange={setPage}
              />
            )}
          </>
        )}
      </div>
    </div>
  );
}
