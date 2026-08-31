"use client";

import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";

interface SidebarContextType {
  isOpen: boolean;
  isHover: boolean;
  isMobileOpen: boolean;
  toggleOpen: () => void;
  setIsOpen: (open: boolean) => void;
  setIsHover: (hover: boolean) => void;
  setIsMobileOpen: (open: boolean) => void;
  toggleMobileOpen: () => void;
  getOpenState: () => boolean;
}

const STORAGE_KEY = "stockledger_sidebar_collapsed";

const SidebarContext = createContext<SidebarContextType | null>(null);

export function SidebarProvider({ children }: { children: React.ReactNode }) {
  const [isOpen, setIsOpenState] = useState<boolean>(true);
  const [isHover, setIsHover] = useState<boolean>(false);
  const [isMobileOpen, setIsMobileOpen] = useState<boolean>(false);
  const [mounted, setMounted] = useState<boolean>(false);

  useEffect(() => {
    setMounted(true);
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved !== null) {
        setIsOpenState(saved !== "true");
      }
    } catch {
      // Ignore localStorage errors
    }
  }, []);

  const setIsOpen = useCallback((open: boolean) => {
    setIsOpenState(open);
    try {
      localStorage.setItem(STORAGE_KEY, (!open).toString());
    } catch {
      // Ignore localStorage errors
    }
  }, []);

  const toggleOpen = useCallback(() => {
    setIsOpenState((prev) => {
      const next = !prev;
      try {
        localStorage.setItem(STORAGE_KEY, (!next).toString());
      } catch {
        // Ignore localStorage errors
      }
      return next;
    });
  }, []);

  const toggleMobileOpen = useCallback(() => {
    setIsMobileOpen((prev) => !prev);
  }, []);

  const getOpenState = useCallback(() => {
    return isOpen || isHover;
  }, [isOpen, isHover]);

  const value = useMemo<SidebarContextType>(
    () => ({
      isOpen: mounted ? isOpen : true,
      isHover,
      isMobileOpen,
      toggleOpen,
      setIsOpen,
      setIsHover,
      setIsMobileOpen,
      toggleMobileOpen,
      getOpenState,
    }),
    [
      isOpen,
      isHover,
      isMobileOpen,
      mounted,
      toggleOpen,
      setIsOpen,
      toggleMobileOpen,
      getOpenState,
    ]
  );

  return (
    <SidebarContext.Provider value={value}>{children}</SidebarContext.Provider>
  );
}

export function useSidebar(): SidebarContextType {
  const context = useContext(SidebarContext);
  if (!context) {
    throw new Error("useSidebar must be used within a SidebarProvider");
  }
  return context;
}
