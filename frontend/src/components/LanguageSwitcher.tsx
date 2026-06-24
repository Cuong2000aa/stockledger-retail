"use client";

import { useLocale } from "next-intl";
import { usePathname, useRouter } from "@/i18n/routing";
import { routing } from "@/i18n/routing";
import clsx from "clsx";

export function LanguageSwitcher({
  variant = "light",
  compact = false,
}: {
  variant?: "light" | "dark";
  compact?: boolean;
}) {
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();

  const isDark = variant === "dark";

  return (
    <div
      className={clsx(
        "inline-flex rounded-md p-0.5 text-xs font-medium",
        compact ? "text-[10px]" : "text-xs",
        isDark ? "bg-white/10 ring-1 ring-white/10" : "bg-slate-100 ring-1 ring-slate-200"
      )}
      role="group"
      aria-label="Language"
    >
      {routing.locales.map((loc) => {
        const active = locale === loc;
        return (
          <button
            key={loc}
            type="button"
            onClick={() => router.replace(pathname, { locale: loc })}
            className={clsx(
              "rounded transition-all",
              compact ? "px-2 py-0.5" : "rounded-md px-2.5 py-1",
              active
                ? isDark
                  ? "bg-white/15 text-white shadow-sm"
                  : "bg-white text-brand-700 shadow-sm"
                : isDark
                  ? "text-slate-400 hover:text-slate-200"
                  : "text-slate-500 hover:text-slate-700"
            )}
          >
            {loc === "vi" ? "VI" : "EN"}
          </button>
        );
      })}
    </div>
  );
}
