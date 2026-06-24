"use client";

import { PageHeader } from "@/components/PageHeader";
import { useNotify } from "@/hooks/useNotify";
import { useAuth } from "@/features/auth/AuthProvider";
import {
  fetchJobHistory,
  fetchOperationsDashboard,
  triggerBackgroundJob,
  updateBackgroundJob,
  type BackgroundJob,
  type BackgroundJobRun,
} from "@/features/operations/api";
import {
  formatTrigger,
  getJobDescription,
  getJobName,
} from "@/features/operations/i18n";
import { formatDate, formatNumber } from "@/lib/format";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/routing";
import clsx from "clsx";
import {
  Activity,
  Clock3,
  Loader2,
  Play,
  RefreshCw,
  ServerCog,
  ShieldAlert,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";

export default function OperationsPage() {
  const t = useTranslations("operations");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { notifyError, notifySuccess } = useNotify();
  const { isSystemAdmin, isLoading: authLoading } = useAuth();
  const [selectedJobKey, setSelectedJobKey] = useState<string | null>(null);
  const [draftIntervals, setDraftIntervals] = useState<Record<string, number>>({});

  useEffect(() => {
    if (!authLoading && !isSystemAdmin) {
      router.replace("/");
    }
  }, [authLoading, isSystemAdmin, router]);

  const { data: dashboard, isLoading, refetch, isFetching } = useQuery({
    queryKey: ["operations-dashboard"],
    queryFn: fetchOperationsDashboard,
    enabled: isSystemAdmin,
    refetchInterval: 10_000,
  });

  const selectedJob = useMemo(
    () => dashboard?.jobs.find((job) => job.jobKey === selectedJobKey) ?? dashboard?.jobs[0],
    [dashboard?.jobs, selectedJobKey]
  );

  const { data: history } = useQuery({
    queryKey: ["operations-job-history", selectedJob?.jobKey],
    queryFn: () => fetchJobHistory(selectedJob!.jobKey, 30),
    enabled: Boolean(selectedJob?.jobKey && isSystemAdmin),
    refetchInterval: 10_000,
  });

  const saveMutation = useMutation({
    mutationFn: ({ jobKey, isEnabled, intervalMinutes }: {
      jobKey: string;
      isEnabled: boolean;
      intervalMinutes: number;
    }) => updateBackgroundJob(jobKey, { isEnabled, intervalMinutes }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["operations-dashboard"] });
      notifySuccess(t("saved"));
    },
    onError: notifyError,
  });

  const runMutation = useMutation({
    mutationFn: triggerBackgroundJob,
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: ["operations-dashboard"] });
      notifySuccess(result.message);
    },
    onError: notifyError,
  });

  if (authLoading || !isSystemAdmin) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center text-slate-500">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        action={
          <button
            type="button"
            className="btn-secondary"
            onClick={() => void refetch()}
            disabled={isFetching}
          >
            <RefreshCw className={clsx("h-4 w-4", isFetching && "animate-spin")} />
            {t("refresh")}
          </button>
        }
      />

      <div className="mb-6 flex items-start gap-3 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
        <ShieldAlert className="mt-0.5 h-4 w-4 shrink-0" />
        <p>{t("adminOnly")}</p>
      </div>

      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2">
          {Array.from({ length: 2 }).map((_, index) => (
            <div key={index} className="card skeleton h-56" />
          ))}
        </div>
      ) : (
        <div className="grid gap-6 xl:grid-cols-[minmax(18rem,24rem)_1fr]">
          <div className="space-y-4">
            {dashboard?.jobs.map((job) => (
              <JobCard
                key={job.jobKey}
                job={job}
                locale={locale}
                active={selectedJob?.jobKey === job.jobKey}
                draftInterval={draftIntervals[job.jobKey] ?? job.intervalMinutes}
                onSelect={() => setSelectedJobKey(job.jobKey)}
                onIntervalChange={(value) =>
                  setDraftIntervals((current) => ({ ...current, [job.jobKey]: value }))
                }
                onToggleEnabled={(enabled) =>
                  saveMutation.mutate({
                    jobKey: job.jobKey,
                    isEnabled: enabled,
                    intervalMinutes: draftIntervals[job.jobKey] ?? job.intervalMinutes,
                  })
                }
                onSave={() =>
                  saveMutation.mutate({
                    jobKey: job.jobKey,
                    isEnabled: job.isEnabled,
                    intervalMinutes: draftIntervals[job.jobKey] ?? job.intervalMinutes,
                  })
                }
                onRun={() => runMutation.mutate(job.jobKey)}
                isSaving={saveMutation.isPending}
                isRunning={runMutation.isPending}
                t={t}
              />
            ))}
          </div>

          <div className="card overflow-hidden">
            <div className="border-b border-slate-100 px-5 py-4">
              <div className="flex items-center gap-2">
                <Activity className="h-4 w-4 text-brand-600" />
                <h2 className="font-semibold text-slate-900">{t("historyTitle")}</h2>
              </div>
              <p className="mt-1 text-sm text-slate-500">
                {selectedJob
                  ? getJobName(t, selectedJob.jobKey, selectedJob.displayName)
                  : t("historySubtitle")}
              </p>
            </div>
            <div className="table-wrap max-h-[34rem] overflow-y-auto scrollbar-thin">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>{t("columns.started")}</th>
                    <th>{t("columns.status")}</th>
                    <th>{t("columns.trigger")}</th>
                    <th>{t("columns.duration")}</th>
                    <th>{t("columns.message")}</th>
                  </tr>
                </thead>
                <tbody>
                  {history?.length ? (
                    history.map((run) => (
                      <HistoryRow key={run.id} run={run} locale={locale} t={t} />
                    ))
                  ) : (
                    <tr>
                      <td colSpan={5} className="py-10 text-center text-slate-500">
                        {tCommon("noData")}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function JobCard({
  job,
  locale,
  active,
  draftInterval,
  onSelect,
  onIntervalChange,
  onToggleEnabled,
  onSave,
  onRun,
  isSaving,
  isRunning,
  t,
}: {
  job: BackgroundJob;
  locale: string;
  active: boolean;
  draftInterval: number;
  onSelect: () => void;
  onIntervalChange: (value: number) => void;
  onToggleEnabled: (enabled: boolean) => void;
  onSave: () => void;
  onRun: () => void;
  isSaving: boolean;
  isRunning: boolean;
  t: ReturnType<typeof useTranslations<"operations">>;
}) {
  return (
    <div
      className={clsx(
        "card cursor-pointer p-5 transition-all",
        active && "ring-2 ring-brand-400"
      )}
      onClick={onSelect}
    >
      <div className="mb-3 flex items-start justify-between gap-3">
        <div className="flex items-start gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-slate-900 text-white">
            <ServerCog className="h-5 w-5" />
          </div>
          <div>
            <h3 className="font-semibold text-slate-900">
              {getJobName(t, job.jobKey, job.displayName)}
            </h3>
            <p className="mt-1 text-xs text-slate-500">
              {getJobDescription(t, job.jobKey, job.description)}
            </p>
          </div>
        </div>
        <StatusBadge status={job.isRunning ? "running" : job.lastStatus} t={t} />
      </div>

      <div className="mb-4 grid gap-2 text-xs text-slate-600">
        <InfoRow
          icon={Clock3}
          label={t("nextRun")}
          value={job.nextRunAtUtc ? formatDate(job.nextRunAtUtc, locale) : "—"}
        />
        <InfoRow
          icon={Activity}
          label={t("lastRun")}
          value={job.lastRunCompletedAtUtc ? formatDate(job.lastRunCompletedAtUtc, locale) : "—"}
        />
      </div>

      <div className="space-y-3 border-t border-slate-100 pt-4" onClick={(e) => e.stopPropagation()}>
        <label className="flex items-center justify-between gap-3 text-sm">
          <span>{t("enabled")}</span>
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-slate-300"
            checked={job.isEnabled}
            onChange={(e) => onToggleEnabled(e.target.checked)}
          />
        </label>

        <label className="block text-sm text-slate-600">
          <span className="mb-1 block text-xs font-medium uppercase tracking-wide text-slate-500">
            {t("intervalMinutes")}
          </span>
          <input
            type="number"
            min={5}
            className="input"
            value={draftInterval}
            onChange={(e) => onIntervalChange(Number(e.target.value) || 5)}
          />
        </label>

        <div className="flex flex-wrap gap-2">
          <button type="button" className="btn-secondary text-xs" onClick={onSave} disabled={isSaving}>
            {t("save")}
          </button>
          <button
            type="button"
            className="btn-primary text-xs"
            onClick={onRun}
            disabled={isRunning || job.isRunning}
          >
            <Play className="h-3.5 w-3.5" />
            {job.isRunning || job.manualRunRequested ? t("running") : t("runNow")}
          </button>
        </div>
      </div>
    </div>
  );
}

function HistoryRow({
  run,
  locale,
  t,
}: {
  run: BackgroundJobRun;
  locale: string;
  t: ReturnType<typeof useTranslations<"operations">>;
}) {
  return (
    <tr>
      <td className="whitespace-nowrap text-xs">{formatDate(run.startedAtUtc, locale)}</td>
      <td>
        <StatusBadge status={run.status} t={t} />
      </td>
      <td className="text-xs">{formatTrigger(t, run.triggeredBy)}</td>
      <td className="tabular-nums text-xs">
        {run.durationMs != null ? `${formatNumber(run.durationMs, locale)} ms` : "—"}
      </td>
      <td className="max-w-xs text-xs text-slate-600">{run.message ?? "—"}</td>
    </tr>
  );
}

function StatusBadge({
  status,
  t,
}: {
  status: string;
  t: ReturnType<typeof useTranslations<"operations">>;
}) {
  const styles = {
    running: "bg-sky-50 text-sky-700 ring-sky-200",
    succeeded: "bg-emerald-50 text-emerald-700 ring-emerald-200",
    failed: "bg-red-50 text-red-700 ring-red-200",
    idle: "bg-slate-50 text-slate-600 ring-slate-200",
    warning: "bg-amber-50 text-amber-700 ring-amber-200",
  } as const;

  const tone = status in styles ? (status as keyof typeof styles) : "idle";

  return (
    <span className={clsx("badge", styles[tone])}>
      {t(`status.${tone}` as never)}
    </span>
  );
}

function InfoRow({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Clock3;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-center gap-2">
      <Icon className="h-3.5 w-3.5 text-slate-400" />
      <span className="font-medium text-slate-500">{label}:</span>
      <span>{value}</span>
    </div>
  );
}
