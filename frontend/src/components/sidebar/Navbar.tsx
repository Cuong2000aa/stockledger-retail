"use client";

import { usePathname } from "@/i18n/routing";
import { useTranslations } from "next-intl";
import { useSidebar } from "@/hooks/useSidebar";
import { useAuth } from "@/features/auth/AuthProvider";
import { useWarehouseScope } from "@/hooks/useWarehouseScope";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import {
  Building2,
  ChevronRight,
  Menu,
  ShieldCheck,
  UserCheck,
} from "lucide-react";
import clsx from "clsx";

const PATH_NAME_MAP: Record<string, string> = {
  products: "products",
  "product-variants": "productVariants",
  warehouses: "warehouses",
  suppliers: "suppliers",
  "purchase-orders": "purchaseOrders",
  "goods-receipts": "goodsReceipts",
  insights: "insights",
  reports: "reports",
  "inventory-documents": "inventoryDocuments",
  "current-stocks": "currentStocks",
  "stock-transactions": "stockTransactions",
  "stock-reservations": "stockReservations",
  admin: "adminSection",
  operations: "operations",
  "audit-logs": "auditLogs",
  brands: "brands",
  users: "users",
  teams: "teams",
  permissions: "permissions",
  "transfer-policies": "transferPolicies",
  "markdownPolicies": "markdownPolicies",
  "markdown-policies": "markdownPolicies",
};

export function Navbar() {
  const tNav = useTranslations("nav");
  const pathname = usePathname();
  const { toggleMobileOpen } = useSidebar();
  const { session, isSystemAdmin } = useAuth();
  const { canSelectAllWarehouses, warehouseIds } = useWarehouseScope();

  // Compute breadcrumbs
  const segments = pathname.split("/").filter(Boolean);
  const breadcrumbs = segments.map((segment, index) => {
    const key = PATH_NAME_MAP[segment];
    const label = key ? tNav(key) : segment;
    const href = "/" + segments.slice(0, index + 1).join("/");
    return { label, href };
  });

  return (
    <header className="sticky top-0 z-20 flex h-16 w-full items-center justify-between border-b border-slate-200/80 bg-white/85 px-4 backdrop-blur-md transition-all sm:px-6">
      {/* ─── LEFT: MOBILE TOGGLE & BREADCRUMBS ────────────────────────── */}
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={toggleMobileOpen}
          className="flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-700 shadow-sm transition hover:bg-slate-50 lg:hidden"
          aria-label="Toggle Navigation"
        >
          <Menu className="h-5 w-5" />
        </button>

        {/* Breadcrumbs */}
        <nav className="flex items-center gap-1.5 text-xs text-slate-500 sm:text-sm">
          <span className="font-medium text-slate-700">{tNav("dashboard")}</span>
          {breadcrumbs.map((crumb, idx) => (
            <div key={idx} className="flex items-center gap-1.5">
              <ChevronRight className="h-3.5 w-3.5 text-slate-400" />
              <span
                className={clsx(
                  "font-medium",
                  idx === breadcrumbs.length - 1
                    ? "font-semibold text-brand-700"
                    : "text-slate-600"
                )}
              >
                {crumb.label}
              </span>
            </div>
          ))}
        </nav>
      </div>

      {/* ─── RIGHT: ACTIONS & STATUS BADGES ───────────────────────────── */}
      <div className="flex items-center gap-2.5 sm:gap-3">
        {/* Warehouse Scope Status Badge */}
        <div
          className="hidden items-center gap-1.5 rounded-full border border-slate-200/80 bg-slate-50/80 px-3 py-1 text-xs font-medium text-slate-700 md:flex"
          title={
            canSelectAllWarehouses
              ? "Quyền truy cập toàn bộ hệ thống kho"
              : `Phụ trách ${warehouseIds.length} kho`
          }
        >
          <Building2 className="h-3.5 w-3.5 text-brand-600" />
          <span>
            {canSelectAllWarehouses
              ? tNav("allWarehouses")
              : `${tNav("assignedWarehouses")} (${warehouseIds.length})`}
          </span>
        </div>

        {/* Language Switcher */}
        <div className="rounded-xl border border-slate-200/80 bg-white p-0.5 shadow-sm">
          <LanguageSwitcher />
        </div>

        {/* User Pill / Role Badge */}
        {session && (
          <div className="hidden items-center gap-2 rounded-xl border border-slate-200/80 bg-white py-1.5 pl-2 pr-3 shadow-sm sm:flex">
            <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-600 text-xs font-bold uppercase text-white shadow-sm">
              {(session.displayName || session.email).charAt(0)}
            </div>
            <div className="text-left">
              <div className="max-w-[120px] truncate text-xs font-semibold leading-tight text-slate-800">
                {session.displayName || session.email.split("@")[0]}
              </div>
              <div className="flex items-center gap-1 text-[10px] font-medium text-slate-500">
                {isSystemAdmin ? (
                  <>
                    <ShieldCheck className="h-3 w-3 text-brand-600" />
                    <span>System Admin</span>
                  </>
                ) : (
                  <>
                    <UserCheck className="h-3 w-3 text-slate-400" />
                    <span>Staff</span>
                  </>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </header>
  );
}
