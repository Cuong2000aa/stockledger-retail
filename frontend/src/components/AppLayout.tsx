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
  PackageCheck,
  BarChart3,
  Clock3,
  ShoppingCart,
  Tags,
  ServerCog,
  Truck,
  Warehouse,
  Store,
  Users,
  UsersRound,
  Shield,
  ArrowLeftRight,
  BadgeDollarSign,
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
  { href: "/goods-receipts", icon: PackageCheck, key: "goodsReceipts" },
  { href: "/insights", icon: Lightbulb, key: "insights" },
  { href: "/reports", icon: BarChart3, key: "reports" },
  { href: "/inventory-documents", icon: FileText, key: "inventoryDocuments" },
  { href: "/current-stocks", icon: Boxes, key: "currentStocks" },
  { href: "/stock-transactions", icon: History, key: "stockTransactions" },
  { href: "/stock-reservations", icon: Clock3, key: "stockReservations" },
] as const;

const adminNavItems = [
  { href: "/admin/operations", icon: ServerCog, key: "operations" },
  { href: "/admin/audit-logs", icon: History, key: "auditLogs" },
  { href: "/admin/brands", icon: Store, key: "brands" },
  { href: "/admin/users", icon: Users, key: "users" },
  { href: "/admin/teams", icon: UsersRound, key: "teams" },
  { href: "/admin/permissions", icon: Shield, key: "permissions" },
  { href: "/admin/transfer-policies", icon: ArrowLeftRight, key: "transferPolicies" },
  { href: "/admin/markdown-policies", icon: BadgeDollarSign, key: "markdownPolicies" },
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
  const { session, logout, isSystemAdmin, isLoading } = useAuth();
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
      if (!isLoading && isSystemAdmin) {
        adminNavItems.forEach(({ href }) => warmRoute(href));
      }
    };

    if (typeof window !== "undefined" && "requestIdleCallback" in window) {
      const id = window.requestIdleCallback(run);
      return () => window.cancelIdleCallback(id);
    }

    const id = globalThis.setTimeout(run, 800);
    return () => globalThis.clearTimeout(id);
  }, [warmRoute, isSystemAdmin, isLoading]);

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
          {!isLoading && isSystemAdmin && (
            <>
              <p className="mb-2 mt-4 px-3 text-[10px] font-semibold uppercase tracking-widest text-slate-500">
                {t("adminSection")}
              </p>
              {adminNavItems.map(({ href, icon: Icon, key }) => {
                const active = pathname.startsWith(href);
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
            </>
          )}
        </nav>

        <div className="shrink-0 border-t border-white/10 px-2.5 py-2">
          {session && (
            <div className="flex items-center gap-2 rounded-lg bg-white/5 px-2 py-1.5 ring-1 ring-white/10">
              <div
                className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-brand-500 to-brand-700 text-[10px] font-bold text-white"
                title={session.email}
              >
                {(session.displayName || session.email).charAt(0).toUpperCase()}
              </div>
              <p
                className="min-w-0 flex-1 truncate text-xs font-medium text-slate-200"
                title={session.email}
              >
                {session.email}
              </p>
              <button
                type="button"
                onClick={logout}
                title={tAuth("signOut")}
                className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md text-slate-400 transition hover:bg-white/10 hover:text-white"
              >
                <LogOut className="h-3.5 w-3.5" />
              </button>
            </div>
          )}
          <div className="mt-1.5 flex items-center justify-between gap-2 px-0.5">
            <span className="text-[10px] text-slate-500">{tCommon("language")}</span>
            <LanguageSwitcher variant="dark" compact />
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
