import { apiClient } from "@/lib/api";

export interface BackgroundJob {
  jobKey: string;
  displayName: string;
  description?: string;
  isEnabled: boolean;
  intervalMinutes: number;
  lastStatus: string;
  lastMessage?: string;
  lastRunStartedAtUtc?: string;
  lastRunCompletedAtUtc?: string;
  nextRunAtUtc?: string;
  isRunning: boolean;
  manualRunRequested: boolean;
}

export interface BackgroundJobRun {
  id: string;
  jobKey: string;
  triggeredBy: string;
  status: string;
  message?: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  durationMs?: number;
}

export interface OperationsDashboard {
  jobs: BackgroundJob[];
  recentRuns: BackgroundJobRun[];
}

export const fetchOperationsDashboard = () =>
  apiClient.get<OperationsDashboard>("/api/admin/operations").then((r) => r.data);

export const fetchJobHistory = (jobKey: string, limit = 50) =>
  apiClient
    .get<BackgroundJobRun[]>(`/api/admin/operations/jobs/${jobKey}/history`, {
      params: { limit },
    })
    .then((r) => r.data);

export const updateBackgroundJob = (
  jobKey: string,
  body: { isEnabled: boolean; intervalMinutes: number }
) =>
  apiClient
    .put<BackgroundJob>(`/api/admin/operations/jobs/${jobKey}`, body)
    .then((r) => r.data);

export const triggerBackgroundJob = (jobKey: string) =>
  apiClient
    .post<{ jobKey: string; accepted: boolean; message: string }>(
      `/api/admin/operations/jobs/${jobKey}/run`
    )
    .then((r) => r.data);
