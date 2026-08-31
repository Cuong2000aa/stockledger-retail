"use client";

import { ChevronLeft } from "lucide-react";
import clsx from "clsx";

interface SidebarToggleProps {
  isOpen: boolean;
  setIsOpen: () => void;
  title?: string;
}

export function SidebarToggle({
  isOpen,
  setIsOpen,
  title = "Toggle Sidebar",
}: SidebarToggleProps) {
  return (
    <div className="invisible absolute -right-[15px] top-[24px] z-20 lg:visible">
      <button
        onClick={setIsOpen}
        className={clsx(
          "flex h-7 w-7 items-center justify-center rounded-full border border-white/25 bg-surface-sidebar text-white/90 shadow-md transition-all duration-300 hover:bg-surface-sidebar-hover hover:text-white hover:scale-110 active:scale-95 focus:outline-none focus:ring-2 focus:ring-white/30"
        )}
        type="button"
        title={title}
        aria-label={title}
      >
        <ChevronLeft
          className={clsx(
            "h-4 w-4 transition-transform duration-300 ease-in-out",
            !isOpen && "rotate-180"
          )}
        />
      </button>
    </div>
  );
}
