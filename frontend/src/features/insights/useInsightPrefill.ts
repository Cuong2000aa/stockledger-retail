"use client";

import { useSearchParams } from "next/navigation";
import { useEffect, useRef } from "react";

export function useInsightPrefill<T extends Record<string, string | number>>(
  apply: (values: Partial<T>) => void
) {
  const searchParams = useSearchParams();
  const appliedRef = useRef(false);

  useEffect(() => {
    if (appliedRef.current) {
      return;
    }

    const entries = Array.from(searchParams.entries());
    if (!entries.length) {
      return;
    }

    const values: Partial<T> = {};
    entries.forEach(([key, value]) => {
      if (!value) {
        return;
      }

      if (key === "orderedQuantity" || key === "quantity") {
        (values as Record<string, string | number>)[key] = Number(value) || 1;
        return;
      }

      (values as Record<string, string | number>)[key] = value;
    });

    if (Object.keys(values).length > 0) {
      apply(values);
      appliedRef.current = true;
    }
  }, [apply, searchParams]);
}
