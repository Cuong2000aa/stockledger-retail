"use client";

import { useLinkStatus } from "next/link";
import { Link, usePathname, useRouter } from "@/i18n/routing";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { useAuth } from "@/features/auth/AuthProvider";
import { prefetchHotNavRoutes, prefetchRouteData } from "@/lib/nav-prefetch";
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
        <span className="absolute left-0 top-1/2 h-6 w-1 -translate-y-1/2 rounded-r-full bg-white" />
      )}
      {pending ? (
        <Loader2 className="h-4 w-4 shrink-0 animate-spin text-white/90" />
      ) : (
        <Icon
          className={clsx(
            "h-4 w-4 shrink-0",
            active ? "text-white" : "text-white/85"
          )}
        />
      )}
      <span
        className={clsx(
          pending && "opacity-80",
          !active && "text-white/90"
        )}
      >
        {label}
      </span>
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
      prefetchHotNavRoutes(queryClient);
    };

    if (typeof window !== "undefined" && "requestIdleCallback" in window) {
      const id = window.requestIdleCallback(run, { timeout: 2500 });
      return () => window.cancelIdleCallback(id);
    }

    const id = globalThis.setTimeout(run, 1500);
    return () => globalThis.clearTimeout(id);
  }, [queryClient]);

  return (
    <div className="flex min-h-screen bg-slate-100">
      <aside className="fixed inset-y-0 left-0 z-30 flex w-[var(--sidebar-width)] flex-col border-r border-black/25 bg-surface-sidebar text-white shadow-xl">
        <div className="border-b border-white/12 px-4 py-4">
          <Link href="/" prefetch className="group flex items-center gap-3">
            <Image
              src="/logo-icon.png?v=3"
              alt={tCommon("appName")}
              width={48}
              height={48}
              className="h-11 w-11 shrink-0 rounded-xl object-cover shadow-sm ring-1 ring-white/20 transition group-hover:ring-white/35"
              priority
            />
            <div className="min-w-0 flex flex-col">
              <span className="text-sm font-bold uppercase leading-tight tracking-wide text-white">
                Stock Ledger
              </span>
              <span className="text-[10px] font-medium uppercase leading-tight tracking-wider text-white/75">
                Accurate · Control · Grow
              </span>
            </div>
          </Link>
        </div>

        <nav className="scrollbar-thin flex-1 space-y-0.5 overflow-y-auto p-3">
          <p className="mb-2 px-3 text-[10px] font-bold uppercase tracking-widest text-white/55">
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
                    ? "bg-white/18 font-semibold text-white shadow-sm ring-1 ring-white/25"
                    : "text-white/90 hover:bg-white/12 hover:text-white"
                )}
              >
                <NavItemLabel icon={Icon} label={t(key)} active={active} />
              </Link>
            );
          })}
          {!isLoading && isSystemAdmin && (
            <>
              <p className="mb-2 mt-4 px-3 text-[10px] font-bold uppercase tracking-widest text-white/55">
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
                        ? "bg-white/18 font-semibold text-white shadow-sm ring-1 ring-white/25"
                        : "text-white/90 hover:bg-white/12 hover:text-white"
                    )}
                  >
                    <NavItemLabel icon={Icon} label={t(key)} active={active} />
                  </Link>
                );
              })}
            </>
          )}
        </nav>

        <div className="shrink-0 border-t border-white/12 px-2.5 py-2">
          {session && (
            <div className="flex items-center gap-2 rounded-lg bg-black/25 px-2 py-1.5 ring-1 ring-white/12">
              <div
                className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-white/90 to-red-100 text-[10px] font-bold text-brand-700"
                title={session.email}
              >
                {(session.displayName || session.email).charAt(0).toUpperCase()}
              </div>
              <p
                className="min-w-0 flex-1 truncate text-xs font-medium text-white/95"
                title={session.email}
              >
                {session.email}
              </p>
              <button
                type="button"
                onClick={logout}
                title={tAuth("signOut")}
                className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md text-white/80 transition hover:bg-white/12 hover:text-white"
              >
                <LogOut className="h-3.5 w-3.5" />
              </button>
            </div>
          )}
          <div className="mt-1.5 flex items-center justify-between gap-2 px-0.5">
            <span className="text-[10px] font-medium text-white/75">{tCommon("language")}</span>
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
