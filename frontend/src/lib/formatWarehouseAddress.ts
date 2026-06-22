import type { Warehouse } from "./types";

type WarehouseAddressParts = {
  addressLine?: string;
  ward?: string;
  district?: string;
  province?: string;
  postalCode?: string;
};

export function formatWarehouseAddress(parts: WarehouseAddressParts): string {
  const locality = [parts.ward, parts.district, parts.province]
    .map((value) => value?.trim())
    .filter(Boolean)
    .join(", ");

  const core = [parts.addressLine?.trim(), locality || undefined]
    .filter(Boolean)
    .join(", ");

  if (!core) {
    return "";
  }

  const postal = parts.postalCode?.trim();
  return postal ? `${core}, ${postal}` : core;
}

export function formatWarehouseLocationSummary(parts: WarehouseAddressParts): string {
  const summary = [parts.district, parts.province]
    .map((value) => value?.trim())
    .filter(Boolean)
    .join(", ");

  return summary || formatWarehouseAddress(parts);
}

export function formatWarehouseOptionLabel(
  warehouse: Pick<Warehouse, "code" | "name" | "district" | "province" | "fullAddress" | "ward" | "addressLine" | "postalCode">
): string {
  const location =
    warehouse.fullAddress ||
    formatWarehouseLocationSummary(warehouse);
  const base = `${warehouse.code} — ${warehouse.name}`;
  return location ? `${base} (${location})` : base;
}
