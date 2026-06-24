import { apiClient } from "@/lib/api";
import type { AppUser, Brand, BrandStatus, PermissionGroup, Team, TransferPolicy } from "@/lib/types";

export const fetchBrands = () =>
  apiClient.get<Brand[]>("/api/brands").then((r) => r.data);

export const createBrand = (body: { code: string; name: string }) =>
  apiClient.post<Brand>("/api/brands", body).then((r) => r.data);

export const updateBrand = (id: string, body: { name: string; status: BrandStatus }) =>
  apiClient.put<Brand>(`/api/brands/${id}`, body).then((r) => r.data);

export const fetchUsers = () =>
  apiClient.get<AppUser[]>("/api/admin/users").then((r) => r.data);

export const createUser = (body: {
  email: string;
  displayName: string;
  groupCodes: string[];
}) => apiClient.post<AppUser>("/api/admin/users", body).then((r) => r.data);

export const updateUser = (
  id: string,
  body: { displayName: string; isActive: boolean; groupCodes: string[] }
) => apiClient.put<AppUser>(`/api/admin/users/${id}`, body).then((r) => r.data);

export const fetchPermissionGroups = () =>
  apiClient.get<PermissionGroup[]>("/api/admin/permissions/groups").then((r) => r.data);

export const fetchTeams = () =>
  apiClient.get<Team[]>("/api/admin/teams").then((r) => r.data);

export const createTeam = (body: {
  code: string;
  name: string;
  leaderUserId: string;
  memberUserIds: string[];
}) => apiClient.post<Team>("/api/admin/teams", body).then((r) => r.data);

export const updateTeam = (
  id: string,
  body: {
    name: string;
    leaderUserId: string;
    isActive: boolean;
    memberUserIds: string[];
  }
) => apiClient.put<Team>(`/api/admin/teams/${id}`, body).then((r) => r.data);

export const fetchTransferPolicies = () =>
  apiClient.get<TransferPolicy[]>("/api/admin/transfer-policies").then((r) => r.data);

export const createTransferPolicy = (body: {
  sourceBrandId?: string;
  destinationBrandId?: string;
  allowCrossBrand: boolean;
  note?: string;
}) => apiClient.post<TransferPolicy>("/api/admin/transfer-policies", body).then((r) => r.data);

export const updateTransferPolicy = (
  id: string,
  body: { allowCrossBrand: boolean; isActive: boolean; note?: string }
) =>
  apiClient
    .put<TransferPolicy>(`/api/admin/transfer-policies/${id}`, body)
    .then((r) => r.data);
