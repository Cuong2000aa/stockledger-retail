"use client";

import { Link, usePathname } from "@/i18n/routing";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import {
  Boxes,
  FileText,
  History,
  LayoutDashboard,
  Lightbulb,
  Package,
  ShoppingCart,
  Tags,
  Truck,
  Warehouse,
} from "lucide-react";
import { useTranslations } from "next-intl";
import clsx from "clsx";
import Image from "next/image";

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

export function AppLayout({ children }: { children: React.ReactNode }) {
  const t = useTranslations("nav");
  const tCommon = useTranslations("common");
  const pathname = usePathname();

  return (
    <div className="flex min-h-screen">
      <aside className="flex w-64 flex-col border-r border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-5 py-4">
          <Link href="/" className="flex items-center gap-3">
            <Image
              src="/logo.png"
              alt={tCommon("appName")}
              width={40}
              height={40}
              className="h-10 w-10 shrink-0 object-contain"
              priority
            />
            <div className="min-w-0 flex flex-col">
              <span className="text-lg font-bold leading-tight text-brand-700">
                {tCommon("appBrand")}
              </span>
              <span className="text-xs font-medium leading-tight text-slate-500">
                {tCommon("appTagline")}
              </span>
            </div>
          </Link>
        </div>
        <nav className="flex-1 space-y-1 p-3">
          {navItems.map(({ href, icon: Icon, key }) => {
            const active =
              href === "/"
                ? pathname === "/"
                : pathname.startsWith(href);
            return (
              <Link
                key={href}
                href={href}
                className={clsx(
                  "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                  active
                    ? "bg-brand-50 text-brand-700"
                    : "text-slate-600 hover:bg-slate-50"
                )}
              >
                <Icon className="h-4 w-4" />
                {t(key)}
              </Link>
            );
          })}
        </nav>
        <div className="border-t border-slate-200 p-4">
          <p className="mb-2 text-xs text-slate-500">{tCommon("language")}</p>
          <LanguageSwitcher />
        </div>
      </aside>
      <main className="flex-1 overflow-auto p-6">{children}</main>
    </div>
  );
}
