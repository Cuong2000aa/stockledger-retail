"use client";

import { useAuth } from "@/features/auth/AuthProvider";
import { isSystemAdminSession } from "@/lib/auth-session";
import { useMemo } from "react";

const ALL_WAREHOUSES_PERMISSION = "inventory.scope.all_warehouses";

export function useWarehouseScope() {
  const { session, hasPermission } = useAuth();

  return useMemo(() => {
    const canSelectAllWarehouses =
      isSystemAdminSession(session) || hasPermission(ALL_WAREHOUSES_PERMISSION);

    const warehouseIds = session?.warehouseIds ?? [];
    const primaryWarehouseId = session?.primaryWarehouseId ?? null;

    const defaultWarehouseId =
      primaryWarehouseId ??
      (warehouseIds.length === 1 ? warehouseIds[0] : "");

    return {
      canSelectAllWarehouses,
      warehouseIds,
      primaryWarehouseId,
      defaultWarehouseId,
    };
  }, [session, hasPermission]);
}
