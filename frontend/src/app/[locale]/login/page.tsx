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
  const [email, setEmail] = useState("admin@stockledger.local");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login({ email, password });
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-screen">
      <div className="relative hidden w-1/2 overflow-hidden bg-surface-sidebar lg:flex lg:flex-col lg:justify-between">
        <div className="absolute inset-0 bg-gradient-to-br from-brand-950 via-surface-sidebar to-brand-900" />
        <div
          className="absolute inset-0 opacity-35"
          style={{
            backgroundImage:
              "radial-gradient(circle at 20% 80%, rgb(200 16 46 / 0.45) 0%, transparent 50%), radial-gradient(circle at 80% 20%, rgb(255 255 255 / 0.08) 0%, transparent 40%)",
          }}
        />
        <div className="relative z-10 p-10">
          <div className="flex items-center gap-4">
            <Image
              src="/logo-icon.png?v=3"
              alt={tCommon("appName")}
              width={72}
              height={72}
              className="h-16 w-16 shrink-0 rounded-2xl object-cover shadow-lg ring-1 ring-white/25"
              priority
            />
            <div>
              <p className="text-2xl font-bold uppercase tracking-wide text-white">Stock Ledger</p>
              <p className="mt-1 text-sm font-medium uppercase tracking-wider text-white/75">
                Accurate · Control · Grow
              </p>
            </div>
          </div>
        </div>
        <div className="relative z-10 p-10">
          <blockquote className="max-w-md text-lg font-medium leading-relaxed text-white/90">
            {t("loginTagline")}
          </blockquote>
          <p className="mt-4 text-sm text-white/65">StockLedger Retail Platform</p>
        </div>
      </div>

      <div className="flex flex-1 flex-col items-center justify-center bg-slate-50 px-4 py-10">
        <div className="w-full max-w-md animate-slide-up">
          <div className="mb-8 text-center lg:hidden">
            <Image
              src="/logo-icon.png?v=3"
              alt={tCommon("appName")}
              width={64}
              height={64}
              className="mx-auto mb-3 h-16 w-16 rounded-2xl object-cover shadow-card ring-1 ring-slate-200/80"
              priority
            />
            <h1 className="text-xl font-bold uppercase tracking-wide text-slate-900">Stock Ledger</h1>
          </div>

          <div className="card p-8 shadow-glow">
            <div className="mb-6">
              <h2 className="text-xl font-bold text-slate-900">{t("signIn")}</h2>
              <p className="mt-1 text-sm text-slate-500">{t("subtitle")}</p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label
                  htmlFor="email"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  {t("email")}
                </label>
                <div className="relative">
                  <User className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <input
                    id="email"
                    type="email"
                    autoComplete="username"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
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

            <p className="mt-6 text-center text-xs text-slate-400">{t("loginHint")}</p>
          </div>

          <div className="mt-6 flex justify-center">
            <LanguageSwitcher />
          </div>
        </div>
      </div>
    </div>
  );
}
