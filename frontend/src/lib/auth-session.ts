export type AuthSession = {
  email: string;
  displayName: string;
  permissionCodes: string[];
  groupCodes: string[];
};

const STORAGE_KEY = "stockledger_auth_session";

export function getAuthSession(): AuthSession | null {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function setAuthSession(session: AuthSession): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearAuthSession(): void {
  localStorage.removeItem(STORAGE_KEY);
}

export function hasAuthSession(): boolean {
  return getAuthSession() !== null;
}

export function isSystemAdminSession(session: AuthSession | null | undefined): boolean {
  return Boolean(
    session?.permissionCodes?.some(
      (code) => code.toLowerCase() === "system.admin"
    )
  );
}
