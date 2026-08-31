"use client";

import { useState } from "react";
import { Link } from "@/i18n/routing";
import { ChevronDown, Dot, LucideIcon } from "lucide-react";
import { useTranslations } from "next-intl";
import clsx from "clsx";
import type { SubmenuItem } from "./menu-list";

interface CollapseMenuButtonProps {
  icon: LucideIcon;
  labelKey: string;
  active: boolean;
  submenus: SubmenuItem[];
  isOpen: boolean;
  onWarmRoute?: (href: string) => void;
  onNavigate?: () => void;
}

export function CollapseMenuButton({
  icon: Icon,
  labelKey,
  active,
  submenus,
  isOpen,
  onWarmRoute,
  onNavigate,
}: CollapseMenuButtonProps) {
  const t = useTranslations("nav");
  const isSubmenuActive = submenus.some((submenu) => submenu.active);
  const [isCollapsed, setIsCollapsed] = useState<boolean>(isSubmenuActive);

  if (!isOpen) {
    // Collapsed Mode: Hover Floating Popover
    return (
      <div className="group relative flex w-full justify-center">
        <button
          type="button"
          className={clsx(
            "flex h-10 w-10 items-center justify-center rounded-xl transition-all duration-200",
            active || isSubmenuActive
              ? "bg-white/20 font-semibold text-white shadow-sm ring-1 ring-white/30"
              : "text-white/80 hover:bg-white/12 hover:text-white"
          )}
          title={t(labelKey)}
        >
          <Icon className="h-5 w-5 shrink-0" />
        </button>

        {/* Floating Flyout Menu */}
        <div className="pointer-events-none absolute left-full top-0 z-50 ml-3 min-w-[200px] origin-top-left scale-95 rounded-xl border border-white/15 bg-surface-sidebar p-2 text-white shadow-2xl opacity-0 transition-all duration-200 ease-out group-hover:pointer-events-auto group-hover:scale-100 group-hover:opacity-100 backdrop-blur-md">
          <div className="border-b border-white/10 px-3 py-1.5 text-xs font-bold uppercase tracking-wider text-white/70">
            {t(labelKey)}
          </div>
          <div className="mt-1 space-y-0.5">
            {submenus.map(({ href, labelKey: subKey, active: isItemActive }) => (
              <Link
                key={href}
                href={href}
                prefetch
                onMouseEnter={() => onWarmRoute?.(href)}
                onFocus={() => onWarmRoute?.(href)}
                onClick={onNavigate}
                className={clsx(
                  "flex items-center gap-2 rounded-lg px-2.5 py-1.5 text-xs font-medium transition-colors",
                  isItemActive
                    ? "bg-white/20 text-white font-semibold"
                    : "text-white/85 hover:bg-white/10 hover:text-white"
                )}
              >
                <Dot
                  className={clsx(
                    "h-4 w-4 shrink-0",
                    isItemActive ? "text-white" : "text-white/50"
                  )}
                />
                <span className="truncate">{t(subKey)}</span>
              </Link>
            ))}
          </div>
        </div>
      </div>
    );
  }

  // Expanded Mode: Accordion
  return (
    <div className="w-full">
      <button
        type="button"
        onClick={() => setIsCollapsed((prev) => !prev)}
        className={clsx(
          "group flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-200",
          active || isSubmenuActive
            ? "bg-white/15 text-white shadow-sm ring-1 ring-white/20"
            : "text-white/85 hover:bg-white/10 hover:text-white"
        )}
      >
        <div className="flex items-center gap-3">
          <Icon
            className={clsx(
              "h-4 w-4 shrink-0",
              active || isSubmenuActive ? "text-white" : "text-white/80"
            )}
          />
          <span className="truncate text-left">{t(labelKey)}</span>
        </div>
        <ChevronDown
          className={clsx(
            "h-4 w-4 shrink-0 text-white/60 transition-transform duration-200",
            isCollapsed && "rotate-180 text-white"
          )}
        />
      </button>

      {/* Accordion Content */}
      <div
        className={clsx(
          "overflow-hidden transition-all duration-200 ease-in-out",
          isCollapsed ? "max-h-60 opacity-100 pt-1" : "max-h-0 opacity-0"
        )}
      >
        <div className="ml-4 space-y-0.5 border-l border-white/15 pl-2">
          {submenus.map(({ href, labelKey: subKey, active: isItemActive }) => (
            <Link
              key={href}
              href={href}
              prefetch
              onMouseEnter={() => onWarmRoute?.(href)}
              onFocus={() => onWarmRoute?.(href)}
              onClick={onNavigate}
              className={clsx(
                "relative flex items-center gap-2 rounded-lg px-2.5 py-2 text-xs font-medium transition-colors",
                isItemActive
                  ? "bg-white/18 text-white font-semibold shadow-sm"
                  : "text-white/75 hover:bg-white/10 hover:text-white"
              )}
            >
              <Dot
                className={clsx(
                  "h-4 w-4 shrink-0",
                  isItemActive ? "text-white scale-125" : "text-white/40"
                )}
              />
              <span className="truncate">{t(subKey)}</span>
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}
