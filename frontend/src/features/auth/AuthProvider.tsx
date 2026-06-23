"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { usePathname, useRouter } from "@/i18n/routing";
import { AppLayout } from "@/components/AppLayout";
import {
  clearAuthSession,
  getAuthSession,
  setAuthSession,
  type AuthSession,
} from "@/lib/auth-session";
import { apiClient } from "@/lib/api";

type LoginInput = { username: string; password: string };

type AuthContextValue = {
  session: AuthSession | null;
  isLoading: boolean;
  login: (input: LoginInput) => Promise<void>;
  logout: () => void;
  hasPermission: (code: string) => boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return ctx;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const pathname = usePathname();
  const router = useRouter();

  useEffect(() => {
    setSession(getAuthSession());
    setIsLoading(false);
  }, []);

  const login = useCallback(
    async (input: LoginInput) => {
      const { data } = await apiClient.post<AuthSession>("/api/auth/login", input);
      const nextSession: AuthSession = {
        email: data.email,
        displayName: data.displayName,
        permissionCodes: data.permissionCodes ?? [],
        groupCodes: data.groupCodes ?? [],
      };
      setAuthSession(nextSession);
      setSession(nextSession);
      router.replace("/");
    },
    [router]
  );

  const logout = useCallback(() => {
    clearAuthSession();
    setSession(null);
    router.replace("/login");
  }, [router]);

  const hasPermission = useCallback(
    (code: string) => {
      if (!session) {
        return false;
      }
      if (session.permissionCodes.includes("system.admin")) {
        return true;
      }
      return session.permissionCodes.includes(code);
    },
    [session]
  );

  const value = useMemo(
    () => ({ session, isLoading, login, logout, hasPermission }),
    [session, isLoading, login, logout, hasPermission]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function AuthShell({ children }: { children: React.ReactNode }) {
  const { session, isLoading } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const isLoginPage = pathname === "/login";

  useEffect(() => {
    if (isLoading) {
      return;
    }
    if (!session && !isLoginPage) {
      router.replace("/login");
    }
    if (session && isLoginPage) {
      router.replace("/");
    }
  }, [session, isLoading, isLoginPage, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50">
        <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-brand-600 shadow-glow">
          <div className="h-6 w-6 animate-spin rounded-full border-2 border-white/30 border-t-white" />
        </div>
        <p className="text-sm font-medium text-slate-500">StockLedger</p>
      </div>
    );
  }

  if (isLoginPage) {
    return <>{children}</>;
  }

  if (!session) {
    return null;
  }

  return <AppLayout>{children}</AppLayout>;
}
