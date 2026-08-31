"use client";

import { useLinkStatus } from "next/link";
import { Link, usePathname } from "@/i18n/routing";
import { useTranslations } from "next-intl";
import { Ellipsis, Loader2, LogOut } from "lucide-react";
import clsx from "clsx";
import { getSidebarMenuList, type MenuItem } from "./menu-list";
import { CollapseMenuButton } from "./CollapseMenuButton";
import { useAuth } from "@/features/auth/AuthProvider";

interface SidebarMenuProps {
  isOpen: boolean;
  onWarmRoute?: (href: string) => void;
  onNavigate?: () => void;
}

function NavItemIconAndLabel({
  icon: Icon,
  label,
  active,
  isOpen,
}: {
  icon: MenuItem["icon"];
  label: string;
  active: boolean;
  isOpen: boolean;
}) {
  const { pending } = useLinkStatus();

  if (!isOpen) {
    return (
      <div className="relative flex h-10 w-10 items-center justify-center">
        {pending ? (
          <Loader2 className="h-5 w-5 shrink-0 animate-spin text-white/90" />
        ) : (
          <Icon
            className={clsx(
              "h-5 w-5 shrink-0 transition-transform duration-200",
              active ? "text-white" : "text-white/80"
            )}
          />
        )}
      </div>
    );
  }

  return (
    <>
      {active && (
        <span className="absolute left-0 top-1/2 h-6 w-1 -translate-y-1/2 rounded-r-full bg-white shadow-sm" />
      )}
      {pending ? (
        <Loader2 className="h-4 w-4 shrink-0 animate-spin text-white/90" />
      ) : (
        <Icon
          className={clsx(
            "h-4 w-4 shrink-0",
            active ? "text-white" : "text-white/80"
          )}
        />
      )}
      <span className={clsx("truncate text-left", pending && "opacity-80")}>
        {label}
      </span>
    </>
  );
}

export function SidebarMenu({
  isOpen,
  onWarmRoute,
  onNavigate,
}: SidebarMenuProps) {
  const t = useTranslations("nav");
  const tAuth = useTranslations("auth");
  const pathname = usePathname();
  const { isSystemAdmin, logout, session } = useAuth();
  const menuGroups = getSidebarMenuList(pathname, isSystemAdmin);

  return (
    <div className="flex flex-1 flex-col justify-between overflow-y-auto px-3 py-2 scrollbar-thin">
      <div className="space-y-4">
        {menuGroups.map(({ groupLabelKey, menus }, groupIndex) => (
          <div key={groupIndex} className="space-y-1">
            {/* Group Label */}
            {isOpen ? (
              <p className="px-3 pb-1 text-[10px] font-bold uppercase tracking-wider text-white/50">
                {t(groupLabelKey)}
              </p>
            ) : (
              <div className="flex w-full justify-center py-1">
                <Ellipsis className="h-4 w-4 text-white/35" />
              </div>
            )}

            {/* Menu Items */}
            <div className="space-y-0.5">
              {menus.map((menu, itemIndex) => {
                const { href, labelKey, icon: Icon, active, submenus } = menu;

                if (submenus && submenus.length > 0) {
                  return (
                    <CollapseMenuButton
                      key={itemIndex}
                      icon={Icon}
                      labelKey={labelKey}
                      active={Boolean(active)}
                      submenus={submenus}
                      isOpen={isOpen}
                      onWarmRoute={onWarmRoute}
                      onNavigate={onNavigate}
                    />
                  );
                }

                return (
                  <div key={itemIndex} className="group relative flex w-full">
                    <Link
                      href={href}
                      prefetch
                      onMouseEnter={() => onWarmRoute?.(href)}
                      onFocus={() => onWarmRoute?.(href)}
                      onClick={onNavigate}
                      className={clsx(
                        "relative flex w-full items-center rounded-xl transition-all duration-200",
                        isOpen
                          ? "gap-3 px-3 py-2.5 text-sm font-medium"
                          : "justify-center py-1",
                        active
                          ? "bg-white/18 font-semibold text-white shadow-sm ring-1 ring-white/25"
                          : "text-white/85 hover:bg-white/10 hover:text-white"
                      )}
                    >
                      <NavItemIconAndLabel
                        icon={Icon}
                        label={t(labelKey)}
                        active={Boolean(active)}
                        isOpen={isOpen}
                      />
                    </Link>

                    {/* Floating Tooltip in Collapsed Mode */}
                    {!isOpen && (
                      <div className="pointer-events-none absolute left-full top-1/2 z-50 ml-3 -translate-y-1/2 whitespace-nowrap rounded-lg border border-white/15 bg-surface-sidebar px-2.5 py-1 text-xs font-semibold text-white shadow-xl opacity-0 transition-opacity duration-200 group-hover:opacity-100 backdrop-blur-md">
                        {t(labelKey)}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </div>

      {/* Bottom User Card / Logout */}
      <div className="mt-4 shrink-0 border-t border-white/10 pt-3">
        {session && isOpen && (
          <div className="mb-2 flex items-center gap-2.5 rounded-xl bg-black/25 px-2.5 py-2 ring-1 ring-white/10">
            <div
              className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-white/20 text-xs font-bold uppercase text-white shadow-sm"
              title={session.displayName || session.email}
            >
              {(session.displayName || session.email).charAt(0)}
            </div>
            <div className="min-w-0 flex-1">
              <div className="truncate text-xs font-semibold text-white">
                {session.displayName || session.email}
              </div>
              <div className="truncate text-[10px] text-white/60">
                {session.email}
              </div>
            </div>
          </div>
        )}

        <button
          type="button"
          onClick={logout}
          className={clsx(
            "group relative flex w-full items-center rounded-xl text-white/80 transition-all duration-200 hover:bg-red-950/50 hover:text-red-200",
            isOpen
              ? "gap-3 px-3 py-2 text-xs font-medium ring-1 ring-white/10"
              : "justify-center py-2"
          )}
          title={tAuth("signOut")}
        >
          <LogOut className="h-4 w-4 shrink-0" />
          {isOpen && <span>{tAuth("signOut")}</span>}

          {!isOpen && (
            <div className="pointer-events-none absolute left-full top-1/2 z-50 ml-3 -translate-y-1/2 whitespace-nowrap rounded-lg border border-white/15 bg-surface-sidebar px-2.5 py-1 text-xs font-semibold text-white shadow-xl opacity-0 transition-opacity duration-200 group-hover:opacity-100">
              {tAuth("signOut")}
            </div>
          )}
        </button>
      </div>
    </div>
  );
}
