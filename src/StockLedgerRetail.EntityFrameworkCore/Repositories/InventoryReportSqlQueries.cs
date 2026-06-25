namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

/// <summary>
/// SQL báo cáo tồn — tách riêng để đọc/debug trong SSMS hoặc pgAdmin.
/// Tham số {0},{1},... map theo thứ tự truyền vào SqlQueryRaw trong InventoryReportReadRepository.
/// </summary>
internal static class InventoryReportSqlQueries
{
    /// <summary>
    /// Tổng giá trị tồn + số dòng (SKU/kho có QuantityOnHand &gt; 0).
    /// Params: warehouseId (nullable), brandId (nullable).
    /// </summary>
    public const string InventoryValueTotals = """
        SELECT
            COALESCE(SUM(cs."QuantityOnHand" * COALESCE(pv."CostPrice", 0)), 0) AS "TotalValue",
            COUNT(*)::int AS "TotalLineCount"
        FROM current_stocks cs
        INNER JOIN product_variants pv ON pv."Id" = cs."ProductVariantId"
        WHERE cs."QuantityOnHand" > 0
          AND (COALESCE({0}::uuid, cs."WarehouseId") = cs."WarehouseId")
          AND (COALESCE({1}::uuid, pv."BrandId") = pv."BrandId")
        """;

    /// <summary>
    /// Chi tiết giá trị tồn có phân trang.
    /// Params: warehouseId, brandId, skip, take.
    /// </summary>
    public const string InventoryValueLines = """
        SELECT
            cs."ProductVariantId" AS "ProductVariantId",
            pv."Sku" AS "Sku",
            cs."WarehouseId" AS "WarehouseId",
            w."Code" AS "WarehouseCode",
            cs."QuantityOnHand" AS "QuantityOnHand",
            pv."CostPrice" AS "UnitCost",
            cs."QuantityOnHand" * COALESCE(pv."CostPrice", 0) AS "InventoryValue"
        FROM current_stocks cs
        INNER JOIN product_variants pv ON pv."Id" = cs."ProductVariantId"
        INNER JOIN warehouses w ON w."Id" = cs."WarehouseId"
        WHERE cs."QuantityOnHand" > 0
          AND (COALESCE({0}::uuid, cs."WarehouseId") = cs."WarehouseId")
          AND (COALESCE({1}::uuid, pv."BrandId") = pv."BrandId")
        ORDER BY "InventoryValue" DESC
        OFFSET {2} LIMIT {3}
        """;

    /// <summary>
    /// NXT — tổng giá trị toàn báo cáo (không phân trang).
    /// CTE movements: nhập/xuất trong kỳ. stock: tồn đóng hiện tại. opening = closing - in + out.
    /// Params: fromInclusive, toExclusive, warehouseId.
    /// </summary>
    public const string NxtTotals = """
        WITH movements AS (
            SELECT
                st."ProductVariantId",
                st."WarehouseId",
                COALESCE(SUM(CASE WHEN st."QuantityDelta" > 0 THEN st."QuantityDelta" ELSE 0 END), 0) AS in_qty,
                COALESCE(SUM(CASE WHEN st."QuantityDelta" < 0 THEN -st."QuantityDelta" ELSE 0 END), 0) AS out_qty
            FROM stock_transactions st
            WHERE st."TransactionDate" >= {0}
              AND st."TransactionDate" < {1}
              AND (COALESCE({2}::uuid, st."WarehouseId") = st."WarehouseId")
            GROUP BY st."ProductVariantId", st."WarehouseId"
        ),
        stock AS (
            SELECT
                cs."ProductVariantId",
                cs."WarehouseId",
                cs."QuantityOnHand" AS closing_qty,
                pv."CostPrice" AS unit_cost
            FROM current_stocks cs
            INNER JOIN product_variants pv ON pv."Id" = cs."ProductVariantId"
            WHERE (COALESCE({2}::uuid, cs."WarehouseId") = cs."WarehouseId")
        ),
        keys AS (
            SELECT m."ProductVariantId", m."WarehouseId" FROM movements m
            UNION
            SELECT s."ProductVariantId", s."WarehouseId" FROM stock s WHERE s.closing_qty <> 0
        ),
        lines AS (
            SELECT
                COALESCE(s.closing_qty, 0) - COALESCE(m.in_qty, 0) + COALESCE(m.out_qty, 0) AS opening_qty,
                COALESCE(m.in_qty, 0) AS in_qty,
                COALESCE(m.out_qty, 0) AS out_qty,
                COALESCE(s.closing_qty, 0) AS closing_qty,
                COALESCE(s.unit_cost, 0) AS unit_cost
            FROM keys k
            LEFT JOIN stock s
                ON s."ProductVariantId" = k."ProductVariantId"
               AND s."WarehouseId" = k."WarehouseId"
            LEFT JOIN movements m
                ON m."ProductVariantId" = k."ProductVariantId"
               AND m."WarehouseId" = k."WarehouseId"
        )
        SELECT
            COALESCE(SUM(opening_qty * unit_cost), 0) AS "TotalOpeningValue",
            COALESCE(SUM(in_qty * unit_cost), 0) AS "TotalInValue",
            COALESCE(SUM(out_qty * unit_cost), 0) AS "TotalOutValue",
            COALESCE(SUM(closing_qty * unit_cost), 0) AS "TotalClosingValue",
            COUNT(*)::int AS "TotalLineCount"
        FROM lines
        """;

    /// <summary>
    /// NXT — dòng chi tiết có phân trang, sắp theo giá trị tồn cuối.
    /// Params: fromInclusive, toExclusive, warehouseId, skip, take.
    /// </summary>
    public const string NxtLines = """
        WITH movements AS (
            SELECT
                st."ProductVariantId",
                st."WarehouseId",
                COALESCE(SUM(CASE WHEN st."QuantityDelta" > 0 THEN st."QuantityDelta" ELSE 0 END), 0) AS in_qty,
                COALESCE(SUM(CASE WHEN st."QuantityDelta" < 0 THEN -st."QuantityDelta" ELSE 0 END), 0) AS out_qty
            FROM stock_transactions st
            WHERE st."TransactionDate" >= {0}
              AND st."TransactionDate" < {1}
              AND (COALESCE({2}::uuid, st."WarehouseId") = st."WarehouseId")
            GROUP BY st."ProductVariantId", st."WarehouseId"
        ),
        stock AS (
            SELECT
                cs."ProductVariantId",
                cs."WarehouseId",
                cs."QuantityOnHand" AS closing_qty,
                pv."Sku" AS sku,
                w."Code" AS warehouse_code,
                pv."CostPrice" AS unit_cost
            FROM current_stocks cs
            INNER JOIN product_variants pv ON pv."Id" = cs."ProductVariantId"
            INNER JOIN warehouses w ON w."Id" = cs."WarehouseId"
            WHERE (COALESCE({2}::uuid, cs."WarehouseId") = cs."WarehouseId")
        ),
        keys AS (
            SELECT m."ProductVariantId", m."WarehouseId" FROM movements m
            UNION
            SELECT s."ProductVariantId", s."WarehouseId" FROM stock s WHERE s.closing_qty <> 0
        ),
        lines AS (
            SELECT
                k."ProductVariantId" AS "ProductVariantId",
                COALESCE(s.sku, '') AS "Sku",
                k."WarehouseId" AS "WarehouseId",
                COALESCE(s.warehouse_code, '') AS "WarehouseCode",
                COALESCE(s.closing_qty, 0) - COALESCE(m.in_qty, 0) + COALESCE(m.out_qty, 0) AS "OpeningQuantity",
                COALESCE(m.in_qty, 0) AS "InQuantity",
                COALESCE(m.out_qty, 0) AS "OutQuantity",
                COALESCE(s.closing_qty, 0) AS "ClosingQuantity",
                s.unit_cost AS "UnitCost",
                (COALESCE(s.closing_qty, 0) - COALESCE(m.in_qty, 0) + COALESCE(m.out_qty, 0)) * COALESCE(s.unit_cost, 0) AS "OpeningValue",
                COALESCE(m.in_qty, 0) * COALESCE(s.unit_cost, 0) AS "InValue",
                COALESCE(m.out_qty, 0) * COALESCE(s.unit_cost, 0) AS "OutValue",
                COALESCE(s.closing_qty, 0) * COALESCE(s.unit_cost, 0) AS "ClosingValue"
            FROM keys k
            LEFT JOIN stock s
                ON s."ProductVariantId" = k."ProductVariantId"
               AND s."WarehouseId" = k."WarehouseId"
            LEFT JOIN movements m
                ON m."ProductVariantId" = k."ProductVariantId"
               AND m."WarehouseId" = k."WarehouseId"
        )
        SELECT *
        FROM lines
        ORDER BY "ClosingValue" DESC
        OFFSET {3} LIMIT {4}
        """;
}
