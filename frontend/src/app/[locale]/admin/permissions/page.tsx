"use client";

import { useAuth } from "@/features/auth/AuthProvider";
import { fetchPermissionGroups } from "@/features/admin/api";
import { DataTableCard, EmptyTableState } from "@/components/DataTableCard";
import { PageHeader } from "@/components/PageHeader";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Shield } from "lucide-react";
import { useEffect } from "react";
import { useRouter } from "@/i18n/routing";

export default function AdminPermissionsPage() {
  const t = useTranslations("admin.permissions");
  const tCommon = useTranslations("common");
  const { isSystemAdmin, isLoading: authLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && !isSystemAdmin) router.replace("/");
  }, [authLoading, isSystemAdmin, router]);

  const { data, isLoading } = useQuery({
    queryKey: ["permission-groups"],
    queryFn: fetchPermissionGroups,
    enabled: isSystemAdmin,
  });

  if (authLoading || !isSystemAdmin) return null;

  return (
    <div>
      <PageHeader title={t("title")} subtitle={t("subtitle")} />
      <DataTableCard title={t("title")} icon={Shield}>
        {isLoading ? (
          <p className="p-4 text-slate-500">{tCommon("loading")}</p>
        ) : !data?.length ? (
          <EmptyTableState message={tCommon("noData")} />
        ) : (
          <div className="space-y-4 p-4">
            {data.map((g) => (
              <div key={g.id} className="rounded-lg border border-slate-200 p-4">
                <div className="font-medium">{g.name} <span className="text-slate-400">({g.code})</span></div>
                {g.description && <p className="mt-1 text-sm text-slate-500">{g.description}</p>}
                <p className="mt-2 font-mono text-xs text-slate-600">{g.permissionCodes.join(" · ")}</p>
              </div>
            ))}
          </div>
        )}
      </DataTableCard>
    </div>
  );
}
