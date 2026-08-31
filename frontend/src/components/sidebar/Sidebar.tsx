"use client";

import Image from "next/image";
import { Link } from "@/i18n/routing";
import { useTranslations } from "next-intl";
import clsx from "clsx";
import { useSidebar } from "@/hooks/useSidebar";
import { SidebarToggle } from "./SidebarToggle";
import { SidebarMenu } from "./SidebarMenu";
import { X } from "lucide-react";

interface SidebarProps {
  onWarmRoute?: (href: string) => void;
}

export function Sidebar({ onWarmRoute }: SidebarProps) {
  const t = useTranslations("nav");
  const tCommon = useTranslations("common");
  const {
    isOpen,
    toggleOpen,
    isMobileOpen,
    setIsMobileOpen,
    setIsHover,
  } = useSidebar();

  return (
    <>
      {/* ─── DESKTOP RETRACTABLE SIDEBAR ──────────────────────────── */}
      <aside
        onMouseEnter={() => setIsHover(true)}
        onMouseLeave={() => setIsHover(false)}
        className={clsx(
          "fixed inset-y-0 left-0 z-30 hidden flex-col border-r border-black/25 bg-surface-sidebar text-white shadow-xl transition-all duration-300 ease-in-out lg:flex",
          isOpen ? "w-[260px]" : "w-[76px]"
        )}
      >
        <SidebarToggle
          isOpen={isOpen}
          setIsOpen={toggleOpen}
          title={isOpen ? t("collapseMenu") : t("expandMenu")}
        />

        {/* Brand Header */}
        <div className="flex h-16 shrink-0 items-center border-b border-white/10 px-4">
          <Link
            href="/"
            prefetch
            className={clsx(
              "group flex items-center transition-all duration-300",
              isOpen ? "gap-3" : "justify-center w-full"
            )}
          >
            <Image
              src="/logo-icon.png?v=3"
              alt={tCommon("appName")}
              width={40}
              height={40}
              className="h-9 w-9 shrink-0 rounded-xl object-cover shadow-sm ring-1 ring-white/20 transition group-hover:ring-white/35"
              priority
            />
            {isOpen && (
              <div className="min-w-0 flex flex-col">
                <span className="truncate text-sm font-bold uppercase leading-tight tracking-wide text-white">
                  Stock Ledger
                </span>
                <span className="truncate text-[9px] font-medium uppercase leading-tight tracking-widest text-white/70">
                  Accurate · Control · Grow
                </span>
              </div>
            )}
          </Link>
        </div>

        {/* Scrollable Navigation Menu */}
        <SidebarMenu isOpen={isOpen} onWarmRoute={onWarmRoute} />
      </aside>

      {/* ─── MOBILE SHEET / DRAWER ────────────────────────────────── */}
      {isMobileOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          {/* Backdrop */}
          <div
            className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm transition-opacity"
            onClick={() => setIsMobileOpen(false)}
          />

          {/* Drawer Panel */}
          <aside className="fixed inset-y-0 left-0 flex w-[280px] max-w-[85vw] flex-col border-r border-black/25 bg-surface-sidebar text-white shadow-2xl animate-fade-in">
            <div className="flex h-16 items-center justify-between border-b border-white/10 px-4">
              <Link
                href="/"
                prefetch
                onClick={() => setIsMobileOpen(false)}
                className="group flex items-center gap-3"
              >
                <Image
                  src="/logo-icon.png?v=3"
                  alt={tCommon("appName")}
                  width={36}
                  height={36}
                  className="h-8 w-8 rounded-xl object-cover shadow-sm ring-1 ring-white/20"
                  priority
                />
                <div className="min-w-0 flex flex-col">
                  <span className="text-sm font-bold uppercase text-white">
                    Stock Ledger
                  </span>
                  <span className="text-[9px] font-medium tracking-wider text-white/70">
                    Retail Inventory
                  </span>
                </div>
              </Link>
              <button
                type="button"
                onClick={() => setIsMobileOpen(false)}
                className="rounded-lg p-1.5 text-white/80 hover:bg-white/15 hover:text-white"
              >
                <X className="h-5 w-5" />
              </button>
            </div>

            <SidebarMenu
              isOpen={true}
              onWarmRoute={onWarmRoute}
              onNavigate={() => setIsMobileOpen(false)}
            />
          </aside>
        </div>
      )}
    </>
  );
}
