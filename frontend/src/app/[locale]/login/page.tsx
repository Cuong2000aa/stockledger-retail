"use client";

import { useState } from "react";
import Image from "next/image";
import { useTranslations } from "next-intl";
import { Lock, User } from "lucide-react";
import { useAuth } from "@/features/auth/AuthProvider";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { LoadingSpinner } from "@/components/LoadingState";
import { getApiErrorMessage } from "@/lib/api";

export default function LoginPage() {
  const t = useTranslations("auth");
  const tCommon = useTranslations("common");
  const { login } = useAuth();
  const [username, setUsername] = useState("admin");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login({ username, password });
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-screen">
      <div className="relative hidden w-1/2 overflow-hidden bg-surface-sidebar lg:flex lg:flex-col lg:justify-between">
        <div className="absolute inset-0 bg-gradient-to-br from-brand-950 via-surface-sidebar to-slate-900" />
        <div
          className="absolute inset-0 opacity-30"
          style={{
            backgroundImage:
              "radial-gradient(circle at 20% 80%, rgb(99 102 241 / 0.4) 0%, transparent 50%), radial-gradient(circle at 80% 20%, rgb(56 189 248 / 0.2) 0%, transparent 40%)",
          }}
        />
        <div className="relative z-10 p-10">
          <div className="flex items-center gap-3">
            <Image
              src="/logo.png"
              alt={tCommon("appName")}
              width={48}
              height={48}
              className="h-12 w-12 object-contain"
              priority
            />
            <div>
              <p className="text-xl font-bold text-white">{tCommon("appBrand")}</p>
              <p className="text-sm text-slate-400">{tCommon("appTagline")}</p>
            </div>
          </div>
        </div>
        <div className="relative z-10 p-10">
          <blockquote className="max-w-md text-lg font-medium leading-relaxed text-slate-300">
            {t("loginTagline")}
          </blockquote>
          <p className="mt-4 text-sm text-slate-500">StockLedger Retail Platform</p>
        </div>
      </div>

      <div className="flex flex-1 flex-col items-center justify-center bg-slate-50 px-4 py-10">
        <div className="w-full max-w-md animate-slide-up">
          <div className="mb-8 text-center lg:hidden">
            <Image
              src="/logo.png"
              alt={tCommon("appName")}
              width={56}
              height={56}
              className="mx-auto mb-4 h-14 w-14 object-contain"
              priority
            />
            <h1 className="text-2xl font-bold text-slate-900">{tCommon("appBrand")}</h1>
          </div>

          <div className="card p-8 shadow-glow">
            <div className="mb-6">
              <h2 className="text-xl font-bold text-slate-900">{t("signIn")}</h2>
              <p className="mt-1 text-sm text-slate-500">{t("subtitle")}</p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label
                  htmlFor="username"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  {t("username")}
                </label>
                <div className="relative">
                  <User className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <input
                    id="username"
                    type="text"
                    autoComplete="username"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    className="input pl-10"
                    required
                  />
                </div>
              </div>

              <div>
                <label
                  htmlFor="password"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  {t("password")}
                </label>
                <div className="relative">
                  <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <input
                    id="password"
                    type="password"
                    autoComplete="current-password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="input pl-10"
                    required
                  />
                </div>
              </div>

              {error && (
                <p
                  className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
                  role="alert"
                >
                  {error}
                </p>
              )}

              <button type="submit" disabled={submitting} className="btn-primary w-full">
                {submitting ? (
                  <>
                    <LoadingSpinner />
                    {tCommon("loading")}
                  </>
                ) : (
                  t("signIn")
                )}
              </button>
            </form>

            <p className="mt-6 text-center text-xs text-slate-400">{t("stubHint")}</p>
          </div>

          <div className="mt-6 flex justify-center">
            <LanguageSwitcher />
          </div>
        </div>
      </div>
    </div>
  );
}
