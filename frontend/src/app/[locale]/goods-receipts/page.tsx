"use client";

import { Link } from "@/i18n/routing";
import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { ListFilterBar } from "@/components/ListFilterBar";
import { TableSkeleton } from "@/components/LoadingState";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { fetchGoodsReceipts } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { GoodsReceiptStatus } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { ChevronRight, PackageCheck } from "lucide-react";
import { useState } from "react";

function grStatusLabel(
  status: GoodsReceiptStatus,
  t: ReturnType<typeof useTranslations<"goodsReceipts">>
) {
  switch (status) {
    case GoodsReceiptStatus.Draft:
      return t("statusDraft");
    case GoodsReceiptStatus.Approved:
      return t("statusApproved");
    case GoodsReceiptStatus.Cancelled:
      return t("statusCancelled");
    default:
      return String(status);
  }
}

export default function GoodsReceiptsPage() {
  const t = useTranslations("goodsReceipts");
  const tCommon = useTranslations("common");
  const tFilters = useTranslations("filters");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<GoodsReceiptStatus | "">("");

  const { data, isLoading } = useQuery({
    queryKey: ["goods-receipts", page, statusFilter],
    queryFn: () =>
      fetchGoodsReceipts(undefined, statusFilter === "" ? undefined : statusFilter, page, 20),
  });

  return (
    <div>
      <PageHeader title={t("listTitle")} subtitle={t("listSubtitle")} />

      <ListFilterBar variant="enhanced" search="" onSearchChange={() => {}}>
        <select
            className="input-field min-w-[160px]"
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value === "" ? "" : Number(e.target.value));
              setPage(1);
            }}
          >
            <option value="">{tFilters("allStatuses")}</option>
            <option value={GoodsReceiptStatus.Draft}>{t("statusDraft")}</option>
            <option value={GoodsReceiptStatus.Approved}>{t("statusApproved")}</option>
            <option value={GoodsReceiptStatus.Cancelled}>{t("statusCancelled")}</option>
          </select>
      </ListFilterBar>

      <DataTableCard title={t("listTitle")} icon={PackageCheck}>
        {isLoading ? (
          <TableSkeleton rows={6} cols={5} />
        ) : !data?.items.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>{t("grNo")}</th>
                  <th>{t("poNo")}</th>
                  <th>{tCommon("status")}</th>
                  <th>{t("receiptDate")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.items.map((gr) => (
                  <tr key={gr.id}>
                    <td>
                      <CodePill>{gr.grNo}</CodePill>
                    </td>
                    <td>{gr.poNo}</td>
                    <td>{grStatusLabel(gr.status, t)}</td>
                    <td>{formatDate(gr.receiptDate, locale)}</td>
                    <td className="text-right">
                      <Link
                        href={`/goods-receipts/${gr.id}`}
                        className="inline-flex items-center gap-1 text-sm text-brand-600 hover:underline"
                      >
                        {tCommon("actions")}
                        <ChevronRight className="h-4 w-4" />
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {data && data.totalCount > data.pageSize && (
          <Pagination
            page={page}
            pageSize={data.pageSize}
            totalCount={data.totalCount}
            onChange={setPage}
          />
        )}
      </DataTableCard>
    </div>
  );
}
