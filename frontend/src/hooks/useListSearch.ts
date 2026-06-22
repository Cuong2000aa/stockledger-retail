"use client";

import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { useEffect, useRef, useState } from "react";

export function useListSearch(onDebouncedChange?: () => void, delayMs = 300) {
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search, delayMs);
  const onChangeRef = useRef(onDebouncedChange);

  useEffect(() => {
    onChangeRef.current = onDebouncedChange;
  });

  useEffect(() => {
    onChangeRef.current?.();
  }, [debouncedSearch]);

  const resetSearch = () => setSearch("");

  return {
    search,
    setSearch,
    debouncedSearch,
    resetSearch,
    hasSearch: search.length > 0,
  };
}
