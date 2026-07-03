"use client";

import { QueryClient, QueryClientProvider, keepPreviousData } from "@tanstack/react-query";
import { useState } from "react";
import { LIST_STALE_TIME } from "@/lib/query-config";

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: LIST_STALE_TIME,
        gcTime: 15 * 60 * 1000,
        refetchOnWindowFocus: false,
        refetchOnReconnect: true,
        retry: 1,
        placeholderData: keepPreviousData,
      },
    },
  });
}

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [client] = useState(createQueryClient);
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}
