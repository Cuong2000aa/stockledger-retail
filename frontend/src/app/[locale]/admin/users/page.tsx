"use client";

import { useAuth } from "@/features/auth/AuthProvider";
import { fetchUsers } from "@/features/admin/api";
import { DataTableCard, EmptyTableState } from "@/components/DataTableCard";
import { PageHeader } from "@/components/PageHeader";
import { ActiveBadge } from "@/components/StatusBadge";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Users } from "lucide-react";
import { useEffect } from "react";
import { useRouter } from "@/i18n/routing";

export default function AdminUsersPage() {
  const t = useTranslations("admin.users");
  const tCommon = useTranslations("common");
  const { isSystemAdmin, isLoading: authLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && !isSystemAdmin) router.replace("/");
  }, [authLoading, isSystemAdmin, router]);

  const { data, isLoading } = useQuery({
    queryKey: ["admin-users"],
    queryFn: fetchUsers,
    enabled: isSystemAdmin,
  });

  if (authLoading || !isSystemAdmin) return null;

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />
      <DataTableCard title={t("title")} icon={Users}>
        {isLoading ? (
          <p className="p-4 text-slate-500">{tCommon("loading")}</p>
        ) : !data?.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>{tCommon("status")}</th>
                  <th>Groups</th>
                </tr>
              </thead>
              <tbody>
                {data.map((u) => (
                  <tr key={u.id}>
                    <td>
                      <div className="font-medium">{u.displayName}</div>
                      <div className="text-xs text-slate-500">{u.email}</div>
                    </td>
                    <td>
                      <ActiveBadge
                        active={u.isActive}
                        label={u.isActive ? tCommon("active") : tCommon("inactive")}
                      />
                    </td>
                    <td className="text-xs">{u.groupCodes.join(", ") || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </DataTableCard>
    </div>
  );
}
