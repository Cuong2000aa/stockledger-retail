"use client";

import { Link } from "@/i18n/routing";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { fetchPurchaseOrders } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { PurchaseOrderStatus } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

function poStatusLabel(
  status: PurchaseOrderStatus,
  t: ReturnType<typeof useTranslations<"purchaseOrders">>
) {
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
    default:
      return String(status);
  }
}

export default function PurchaseOrdersPage() {
  const t = useTranslations("purchaseOrders");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<PurchaseOrderStatus | "">("");

  const { data, isLoading } = useQuery({
    queryKey: ["purchase-orders", page, statusFilter],
    queryFn: () =>
      fetchPurchaseOrders(
        statusFilter === "" ? undefined : statusFilter,
        undefined,
        page
      ),
  });

  return (
    <div>
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <Link href="/purchase-orders/new" className="btn-primary">
            + {t("create")}
          </Link>
        }
      />

      <div className="mb-4">
        <select
          className="input max-w-xs"
          value={statusFilter}
          onChange={(e) => {
            setStatusFilter(
              e.target.value === "" ? "" : Number(e.target.value)
            );
            setPage(1);
          }}
        >
          <option value="">{tCommon("status")}: All</option>
          {Object.values(PurchaseOrderStatus)
            .filter((v) => typeof v === "number")
            .map((s) => (
              <option key={s} value={s}>
                {poStatusLabel(s as PurchaseOrderStatus, t)}
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
                    <th>{t("poNo")}</th>
                    <th>{t("supplier")}</th>
                    <th>{t("orderDate")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{tCommon("actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.map((po) => (
                    <tr key={po.id}>
                      <td className="font-mono text-xs">{po.poNo}</td>
                      <td>
                        {po.supplierCode} — {po.supplierName}
                      </td>
                      <td>{formatDate(po.orderDate, locale)}</td>
                      <td>{poStatusLabel(po.status, t)}</td>
                      <td>
                        <Link
                          href={`/purchase-orders/${po.id}`}
                          className="text-brand-600 hover:underline"
                        >
                          {t("detail")}
                        </Link>
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
