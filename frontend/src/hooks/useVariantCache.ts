"use client";

import { fetchProductVariant, fetchProductVariants } from "@/lib/api";
import { formatVariantOptionLabel } from "@/lib/formatVariantLabel";
import type { ProductVariant } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";

export function useVariantCache(extraVariantIds: string[] = []) {
  const [cache, setCache] = useState<Map<string, ProductVariant>>(
    () => new Map()
  );
  const fetchedIds = useRef(new Set<string>());

  const mergeVariants = useCallback((items: ProductVariant[]) => {
    if (!items.length) return;
    setCache((prev) => {
      const next = new Map(prev);
      let changed = false;
      for (const item of items) {
        next.set(item.id, item);
        changed = true;
      }
      return changed ? next : prev;
    });
  }, []);

  const { data: variants } = useQuery({
    queryKey: ["variants-cache"],
    queryFn: () => fetchProductVariants(1, 500),
    staleTime: 60_000,
  });

  useEffect(() => {
    if (variants?.items) {
      mergeVariants(variants.items);
    }
  }, [variants, mergeVariants]);

  const extraKey = extraVariantIds.filter(Boolean).join("|");

  useEffect(() => {
    const missing = extraVariantIds.filter(
      (id) => id && !fetchedIds.current.has(id)
    );
    if (!missing.length) return;

    let cancelled = false;
    missing.forEach((id) => fetchedIds.current.add(id));

    void Promise.all(missing.map((id) => fetchProductVariant(id)))
      .then((items) => {
        if (!cancelled) mergeVariants(items);
      })
      .catch(() => {
        missing.forEach((id) => fetchedIds.current.delete(id));
      });

    return () => {
      cancelled = true;
    };
  }, [extraKey, mergeVariants, extraVariantIds]);

  const loadVariantOptions = useCallback(
    async (search: string) => {
      const result = await fetchProductVariants(1, 50, search || undefined);
      mergeVariants(result.items);
      return result.items.map((v) => ({
        id: v.id,
        label: formatVariantOptionLabel(v),
      }));
    },
    [mergeVariants]
  );

  return {
    variantById: cache,
    mergeVariants,
    loadVariantOptions,
    variants: variants?.items ?? [],
  };
}
