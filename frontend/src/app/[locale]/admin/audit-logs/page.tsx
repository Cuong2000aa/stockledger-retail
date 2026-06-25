"use client";

import { DataTableCard, CodePill, EmptyTableState } from "@/components/DataTableCard";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { fetchAuditLogs } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { History } from "lucide-react";
import { useState } from "react";

export default function AdminAuditLogsPage() {
  const t = useTranslations("audit");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [entityName, setEntityName] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["audit-logs", page, entityName],
    queryFn: () => fetchAuditLogs(entityName || undefined, undefined, page, 20),
  });

  return (
    <div className="space-y-6">
      <PageHeader title={t("logsTitle")} subtitle={t("logsSubtitle")} />

      <div className="card p-4">
        <label className="block text-sm text-slate-600">
          <span className="mb-1 block text-xs font-medium uppercase tracking-wide text-slate-500">
            {t("filterEntity")}
          </span>
          <input
            className="input-field max-w-md"
            value={entityName}
            onChange={(e) => {
              setEntityName(e.target.value);
              setPage(1);
            }}
            placeholder="GoodsReceipt"
          />
        </label>
      </div>

      <DataTableCard title={t("logsTitle")} icon={History} count={data?.totalCount}>
        {isLoading ? (
          <p className="p-6 text-center text-sm text-slate-500">{tCommon("loading")}</p>
        ) : !data?.items.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <>
            <div className="table-wrap max-h-[36rem] overflow-auto">
              <table className="data-table text-sm">
                <thead className="sticky top-0 z-10 bg-white">
                  <tr>
                    <th>{t("entityName")}</th>
                    <th>{t("action")}</th>
                    <th>{t("createdBy")}</th>
                    <th>{t("createdAt")}</th>
                    <th>{t("ip")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((log) => (
                    <tr key={log.id}>
                      <td>
                        <CodePill>{log.entityName}</CodePill>
                        <p className="mt-1 font-mono text-xs text-slate-500">{log.entityId}</p>
                      </td>
                      <td>{log.action}</td>
                      <td>{log.createdBy}</td>
                      <td className="whitespace-nowrap text-xs text-slate-500">
                        {formatDate(log.createdAt, locale)}
                      </td>
                      <td className="text-xs text-slate-500">{log.ipAddress ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </DataTableCard>
    </div>
  );
}
