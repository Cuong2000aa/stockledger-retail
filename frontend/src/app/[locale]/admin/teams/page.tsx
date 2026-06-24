"use client";

import { useAuth } from "@/features/auth/AuthProvider";
import { fetchTeams } from "@/features/admin/api";
import { DataTableCard, EmptyTableState } from "@/components/DataTableCard";
import { PageHeader } from "@/components/PageHeader";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { UsersRound } from "lucide-react";
import { useEffect } from "react";
import { useRouter } from "@/i18n/routing";

export default function AdminTeamsPage() {
  const t = useTranslations("admin.teams");
  const tCommon = useTranslations("common");
  const { isSystemAdmin, isLoading: authLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && !isSystemAdmin) router.replace("/");
  }, [authLoading, isSystemAdmin, router]);

  const { data, isLoading } = useQuery({
    queryKey: ["admin-teams"],
    queryFn: fetchTeams,
    enabled: isSystemAdmin,
  });

  if (authLoading || !isSystemAdmin) return null;

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />
      <DataTableCard title={t("title")} icon={UsersRound}>
        {isLoading ? (
          <p className="p-4 text-slate-500">{tCommon("loading")}</p>
        ) : !data?.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Name</th>
                  <th>Leader</th>
                  <th>Members</th>
                </tr>
              </thead>
              <tbody>
                {data.map((team) => (
                  <tr key={team.id}>
                    <td>{team.code}</td>
                    <td>{team.name}</td>
                    <td>{team.leaderEmail}</td>
                    <td className="text-xs">{team.members.map((m) => m.email).join(", ")}</td>
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
