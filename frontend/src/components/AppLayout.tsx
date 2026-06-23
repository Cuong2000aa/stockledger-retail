"use client";

import { useLinkStatus } from "next/link";
import { Link, usePathname, useRouter } from "@/i18n/routing";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { useAuth } from "@/features/auth/AuthProvider";
import { prefetchRouteData } from "@/lib/nav-prefetch";
import {
  Boxes,
  FileText,
  History,
  LayoutDashboard,
  Lightbulb,
  LogOut,
  Loader2,
  Package,
  ShoppingCart,
  Tags,
  Truck,
  Warehouse,
} from "lucide-react";
import { useTranslations } from "next-intl";
import { useQueryClient } from "@tanstack/react-query";
import clsx from "clsx";
import Image from "next/image";
import { useCallback, useEffect, useRef } from "react";

const navItems = [
  { href: "/", icon: LayoutDashboard, key: "dashboard" },
  { href: "/products", icon: Package, key: "products" },
  { href: "/product-variants", icon: Tags, key: "productVariants" },
  { href: "/warehouses", icon: Warehouse, key: "warehouses" },
  { href: "/suppliers", icon: Truck, key: "suppliers" },
  { href: "/purchase-orders", icon: ShoppingCart, key: "purchaseOrders" },
  { href: "/insights", icon: Lightbulb, key: "insights" },
  { href: "/inventory-documents", icon: FileText, key: "inventoryDocuments" },
  { href: "/current-stocks", icon: Boxes, key: "currentStocks" },
  { href: "/stock-transactions", icon: History, key: "stockTransactions" },
] as const;

function NavItemLabel({
  icon: Icon,
  label,
  active,
}: {
  icon: typeof LayoutDashboard;
  label: string;
  active: boolean;
}) {
  const { pending } = useLinkStatus();

  return (
    <>
      {active && (
        <span className="absolute left-0 top-1/2 h-6 w-1 -translate-y-1/2 rounded-r-full bg-brand-400" />
      )}
      {pending ? (
        <Loader2 className="h-4 w-4 shrink-0 animate-spin text-brand-300" />
      ) : (
        <Icon
          className={clsx(
            "h-4 w-4 shrink-0",
            active ? "text-brand-300" : "text-slate-500"
          )}
        />
      )}
      <span className={pending ? "opacity-80" : undefined}>{label}</span>
    </>
  );
}

export function AppLayout({ children }: { children: React.ReactNode }) {
  const t = useTranslations("nav");
  const tCommon = useTranslations("common");
  const tAuth = useTranslations("auth");
  const pathname = usePathname();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { session, logout } = useAuth();
  const prefetchedRef = useRef(new Set<string>());

  const warmRoute = useCallback(
    (href: string) => {
      if (prefetchedRef.current.has(href)) {
        return;
      }
      prefetchedRef.current.add(href);
      router.prefetch(href);
      prefetchRouteData(queryClient, href);
    },
    [queryClient, router]
  );

  useEffect(() => {
    const run = () => {
      navItems.forEach(({ href }) => warmRoute(href));
    };

    if (typeof window !== "undefined" && "requestIdleCallback" in window) {
      const id = window.requestIdleCallback(run);
      return () => window.cancelIdleCallback(id);
    }

    const id = globalThis.setTimeout(run, 800);
    return () => globalThis.clearTimeout(id);
  }, [warmRoute]);

  return (
    <div className="flex min-h-screen bg-slate-100">
      <aside className="fixed inset-y-0 left-0 z-30 flex w-[var(--sidebar-width)] flex-col border-r border-slate-800/50 bg-surface-sidebar text-slate-300 shadow-xl">
        <div className="border-b border-white/10 px-5 py-5">
          <Link href="/" prefetch className="flex items-center gap-3 group">
            <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-white/10 ring-1 ring-white/10 transition group-hover:bg-white/15">
              <Image
                src="/logo.png"
                alt={tCommon("appName")}
                width={32}
                height={32}
                className="h-8 w-8 object-contain"
                priority
              />
            </div>
            <div className="min-w-0 flex flex-col">
              <span className="text-base font-bold leading-tight text-white">
                {tCommon("appBrand")}
              </span>
              <span className="text-[11px] font-medium leading-tight text-slate-400">
                {tCommon("appTagline")}
              </span>
            </div>
          </Link>
        </div>

        <nav className="scrollbar-thin flex-1 space-y-0.5 overflow-y-auto p-3">
          <p className="mb-2 px-3 text-[10px] font-semibold uppercase tracking-widest text-slate-500">
            Menu
          </p>
          {navItems.map(({ href, icon: Icon, key }) => {
            const active =
              href === "/" ? pathname === "/" : pathname.startsWith(href);
            return (
              <Link
                key={href}
                href={href}
                prefetch
                onMouseEnter={() => warmRoute(href)}
                onFocus={() => warmRoute(href)}
                className={clsx(
                  "relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-200",
                  active
                    ? "bg-brand-600/20 text-white shadow-sm ring-1 ring-brand-500/30"
                    : "text-slate-400 hover:bg-white/5 hover:text-slate-100"
                )}
              >
                <NavItemLabel icon={Icon} label={t(key)} active={active} />
              </Link>
            );
          })}
        </nav>

        <div className="space-y-3 border-t border-white/10 p-4">
          {session && (
            <div className="rounded-xl bg-white/5 px-3 py-3 ring-1 ring-white/10">
              <div className="mb-2 flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-brand-500 to-brand-700 text-xs font-bold text-white">
                {(session.displayName || session.email).charAt(0).toUpperCase()}
              </div>
              <p className="truncate text-sm font-medium text-white">
                {session.displayName || session.email}
              </p>
              <p className="truncate text-xs text-slate-400">{session.email}</p>
            </div>
          )}
          <button
            type="button"
            onClick={logout}
            className="flex w-full items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-400 transition hover:bg-white/5 hover:text-white"
          >
            <LogOut className="h-4 w-4" />
            {tAuth("signOut")}
          </button>
          <div className="flex items-center justify-between gap-2 px-1">
            <span className="text-xs text-slate-500">{tCommon("language")}</span>
            <LanguageSwitcher variant="dark" />
          </div>
        </div>
      </aside>

      <div className="flex min-h-screen flex-1 flex-col pl-[var(--sidebar-width)]">
        <main className="main-gradient flex-1 overflow-auto">
          <div className="page-shell mx-auto max-w-[1400px] p-6 lg:p-8">{children}</div>
        </main>
      </div>
    </div>
  );
}
