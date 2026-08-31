"use client";

import { useCallback, useEffect, useRef } from "react";
import { useRouter } from "@/i18n/routing";
import { useQueryClient } from "@tanstack/react-query";
import { prefetchHotNavRoutes, prefetchRouteData } from "@/lib/nav-prefetch";
import { SidebarProvider, useSidebar } from "@/hooks/useSidebar";
import { Sidebar } from "./sidebar/Sidebar";
import { Navbar } from "./sidebar/Navbar";
import clsx from "clsx";

function MainLayoutShell({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { isOpen } = useSidebar();
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
    <div className="min-h-screen bg-slate-100/90 text-slate-900">
      {/* ─── RETRACTABLE SIDEBAR ────────────────────────────────────── */}
      <Sidebar onWarmRoute={warmRoute} />

      {/* ─── MAIN CONTENT CONTAINER (DYNAMIC WIDTH) ──────────────────── */}
      <div
        className={clsx(
          "flex min-h-screen flex-col transition-all duration-300 ease-in-out",
          isOpen ? "lg:pl-[260px]" : "lg:pl-[76px]"
        )}
      >
        {/* Sticky Top Navbar */}
        <Navbar />

        {/* Page Main Content */}
        <main className="flex-1 p-4 sm:p-6 lg:p-8">
          <div className="mx-auto max-w-7xl">{children}</div>
        </main>
      </div>
    </div>
  );
}

export function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <SidebarProvider>
      <MainLayoutShell>{children}</MainLayoutShell>
    </SidebarProvider>
  );
}
