"use client";

import { useLocale } from "next-intl";
import { usePathname, useRouter } from "@/i18n/routing";
import { routing } from "@/i18n/routing";

export function LanguageSwitcher() {
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();

  return (
    <select
      className="rounded-lg border border-slate-300 bg-white px-2 py-1.5 text-sm"
      value={locale}
      onChange={(e) => router.replace(pathname, { locale: e.target.value })}
      aria-label="Language"
    >
      {routing.locales.map((loc) => (
        <option key={loc} value={loc}>
          {loc === "vi" ? "Tiếng Việt" : "English"}
        </option>
      ))}
    </select>
  );
}
