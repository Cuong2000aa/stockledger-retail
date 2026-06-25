"use client";

import { FormModal } from "@/components/FormModal";
import { Pagination } from "@/components/Pagination";
import { TableSkeleton } from "@/components/LoadingState";
import { fetchUnitBarcodes } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { UnitBarcodeStatus } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import clsx from "clsx";

type UnitBarcodesModalProps = {
  open: boolean;
  onClose: () => void;
  productVariantId: string;
  warehouseId: string;
  sku: string;
  warehouseName: string;
};

const statusStyles: Record<UnitBarcodeStatus, string> = {
  [UnitBarcodeStatus.InStock]: "bg-emerald-50 text-emerald-800 ring-emerald-200",
  [UnitBarcodeStatus.InTransit]: "bg-amber-50 text-amber-800 ring-amber-200",
  [UnitBarcodeStatus.OutOfStock]: "bg-slate-50 text-slate-600 ring-slate-200",
};

function unitBarcodeStatusKey(status: UnitBarcodeStatus): string {
  switch (status) {
    case UnitBarcodeStatus.InStock:
      return "InStock";
    case UnitBarcodeStatus.InTransit:
      return "InTransit";
    case UnitBarcodeStatus.OutOfStock:
      return "OutOfStock";
    default:
      return "InStock";
  }
}

export function UnitBarcodesModal({
  open,
  onClose,
  productVariantId,
  warehouseId,
  sku,
  warehouseName,
}: UnitBarcodesModalProps) {
  const t = useTranslations("stocks");
  const tCommon = useTranslations("common");
  const tDialog = useTranslations("dialog");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<string>(
    String(UnitBarcodeStatus.InStock)
  );
  const [search, setSearch] = useState("");

  const status =
    statusFilter === ""
      ? undefined
      : (Number(statusFilter) as UnitBarcodeStatus);

  const { data, isLoading } = useQuery({
    queryKey: [
      "unit-barcodes",
      productVariantId,
      warehouseId,
      page,
      statusFilter,
      search,
    ],
    queryFn: () =>
      fetchUnitBarcodes(productVariantId, warehouseId, {
        status,
        page,
        pageSize: 50,
        search: search || undefined,
      }),
    enabled: open && !!productVariantId && !!warehouseId,
  });

  const handleClose = () => {
    setPage(1);
    setSearch("");
    setStatusFilter(String(UnitBarcodeStatus.InStock));
    onClose();
  };

  return (
    <FormModal
      open={open}
      title={t("barcode")}
      onClose={handleClose}
      size="xl"
      footer={
        <button type="button" className="btn-secondary" onClick={handleClose}>
          {tDialog("close")}
        </button>
      }
    >
      <div className="space-y-4">
        <div className="rounded-lg bg-slate-50 px-3 py-2 text-sm text-slate-700">
          <span className="font-mono font-medium">{sku}</span>
          <span className="mx-2 text-slate-400">·</span>
          <span>{warehouseName}</span>
        </div>

        <div className="flex flex-wrap gap-3">
          <input
            type="search"
            className="input min-w-[200px] flex-1"
            placeholder={t("searchBarcode")}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <select
            className="input min-w-[160px]"
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPage(1);
            }}
          >
            <option value="">{t("allBarcodeStatuses")}</option>
            <option value={UnitBarcodeStatus.InStock}>
              {t(`unitBarcodeStatuses.${unitBarcodeStatusKey(UnitBarcodeStatus.InStock)}`)}
            </option>
            <option value={UnitBarcodeStatus.InTransit}>
              {t(`unitBarcodeStatuses.${unitBarcodeStatusKey(UnitBarcodeStatus.InTransit)}`)}
            </option>
            <option value={UnitBarcodeStatus.OutOfStock}>
              {t(`unitBarcodeStatuses.${unitBarcodeStatusKey(UnitBarcodeStatus.OutOfStock)}`)}
            </option>
          </select>
        </div>

        {isLoading ? (
          <TableSkeleton rows={5} cols={3} />
        ) : !data?.items.length ? (
          <p className="py-8 text-center text-sm text-slate-500">
            {tCommon("noData")}
          </p>
        ) : (
          <>
            <div className="table-wrap max-h-80 overflow-y-auto scrollbar-thin">
              <table className="data-table">
                <thead className="sticky top-0 z-10 bg-white">
                  <tr>
                    <th>{t("barcode")}</th>
                    <th>{tCommon("status")}</th>
                    <th>{t("lastUpdated")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((item) => (
                    <tr key={item.id}>
                      <td className="font-mono text-xs">{item.barcode}</td>
                      <td>
                        <span
                          className={clsx(
                            "inline-flex rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset",
                            statusStyles[item.status]
                          )}
                        >
                          {t(
                            `unitBarcodeStatuses.${unitBarcodeStatusKey(item.status)}` as "unitBarcodeStatuses.InStock"
                          )}
                        </span>
                      </td>
                      <td className="whitespace-nowrap text-xs text-slate-500">
                        {formatDate(item.lastUpdatedAt, locale)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </div>
    </FormModal>
  );
}
