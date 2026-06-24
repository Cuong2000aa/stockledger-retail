"use client";

import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";

type Option = { id: string; label: string };

type AsyncSearchSelectProps = {
  value: string;
  onChange: (id: string) => void;
  placeholder?: string;
  emptyLabel?: string;
  queryKeyPrefix: string;
  fetchOptions: (search: string) => Promise<Option[]>;
  className?: string;
};

export function AsyncSearchSelect({
  value,
  onChange,
  placeholder = "Search…",
  emptyLabel = "—",
  queryKeyPrefix,
  fetchOptions,
  className = "input",
}: AsyncSearchSelectProps) {
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(search.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  const { data: options = [], isFetching } = useQuery({
    queryKey: [queryKeyPrefix, debounced],
    queryFn: () => fetchOptions(debounced),
    enabled: debounced.length >= 0,
    staleTime: 60_000,
  });

  return (
    <div className="space-y-1">
      <input
        type="search"
        className={className}
        placeholder={placeholder}
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />
      <select
        className={className}
        value={value}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">{emptyLabel}</option>
        {options.map((opt) => (
          <option key={opt.id} value={opt.id}>
            {opt.label}
          </option>
        ))}
      </select>
      {isFetching && debounced.length > 0 ? (
        <p className="text-xs text-slate-500">…</p>
      ) : null}
    </div>
  );
}
