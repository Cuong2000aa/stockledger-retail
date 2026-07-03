-- Seed demo data: N rows per master/stock table (prefix SEED-).
-- Safe to re-run: deletes previous SEED-* rows first.
-- Run: see scripts/seed-demo-data.ps1

\if :{?seed_count}
\else
\set seed_count 100
\endif
\if :{?brand_count}
\else
\set brand_count :seed_count
\endif

BEGIN;

-- ---------------------------------------------------------------------------
-- Cleanup (child tables first)
-- ---------------------------------------------------------------------------
DELETE FROM stock_transactions
WHERE "TransactionNo" LIKE 'SEED-%'
   OR "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%')
   OR "DocumentId" IN (SELECT "Id" FROM inventory_documents WHERE "DocumentNo" LIKE 'SEED-%');
DELETE FROM goods_receipt_line_barcodes
WHERE "GoodsReceiptLineId" IN (
    SELECT "Id" FROM goods_receipt_lines
    WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%')
);
DELETE FROM goods_receipt_lines
WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%');
DELETE FROM purchase_order_line_barcodes
WHERE "PurchaseOrderLineId" IN (
    SELECT "Id" FROM purchase_order_lines
    WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%')
);
DELETE FROM purchase_order_lines
WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%');
DELETE FROM stock_transaction_barcodes
WHERE "StockTransactionId" IN (
    SELECT "Id" FROM stock_transactions
    WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%')
);
DELETE FROM inventory_document_line_barcodes
WHERE "InventoryDocumentLineId" IN (
    SELECT "Id" FROM inventory_document_lines
    WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%')
);
DELETE FROM current_stocks
WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%');
DELETE FROM current_stocks
WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%');
DELETE FROM inventory_document_lines
WHERE "DocumentId" IN (SELECT "Id" FROM inventory_documents WHERE "DocumentNo" LIKE 'SEED-%');
DELETE FROM inventory_document_lines
WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%');
DELETE FROM variant_unit_barcodes
WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%');
DELETE FROM variant_unit_barcodes
WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%');
DELETE FROM product_prices
WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%');
DELETE FROM product_cost_histories
WHERE "ProductVariantId" IN (SELECT "Id" FROM product_variants WHERE "Sku" LIKE 'SEED-%');
DELETE FROM goods_receipt_line_barcodes
WHERE "GoodsReceiptLineId" IN (
    SELECT "Id" FROM goods_receipt_lines
    WHERE "GoodsReceiptId" IN (
        SELECT "Id" FROM goods_receipts
        WHERE "InventoryDocumentId" IN (
            SELECT "Id" FROM inventory_documents
            WHERE "DestinationWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
               OR "SourceWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
               OR "InTransitWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
        )
    )
);
DELETE FROM goods_receipt_lines
WHERE "GoodsReceiptId" IN (
    SELECT "Id" FROM goods_receipts
    WHERE "InventoryDocumentId" IN (
        SELECT "Id" FROM inventory_documents
        WHERE "DestinationWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
           OR "SourceWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
           OR "InTransitWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
    )
);
DELETE FROM goods_receipts
WHERE "InventoryDocumentId" IN (
    SELECT "Id" FROM inventory_documents
    WHERE "DestinationWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
       OR "SourceWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
       OR "InTransitWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
);
DELETE FROM goods_receipt_line_barcodes
WHERE "GoodsReceiptLineId" IN (
    SELECT "Id" FROM goods_receipt_lines
    WHERE "GoodsReceiptId" IN (
        SELECT "Id" FROM goods_receipts
        WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
           OR "PurchaseOrderId" IN (
               SELECT "Id" FROM purchase_orders
               WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
           )
    )
);
DELETE FROM goods_receipt_lines
WHERE "GoodsReceiptId" IN (
    SELECT "Id" FROM goods_receipts
    WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
       OR "PurchaseOrderId" IN (
           SELECT "Id" FROM purchase_orders
           WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
       )
);
DELETE FROM goods_receipts
WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
   OR "PurchaseOrderId" IN (
       SELECT "Id" FROM purchase_orders
       WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
   );
DELETE FROM purchase_order_line_barcodes
WHERE "PurchaseOrderLineId" IN (
    SELECT "Id" FROM purchase_order_lines
    WHERE "PurchaseOrderId" IN (
        SELECT "Id" FROM purchase_orders
        WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
    )
);
DELETE FROM purchase_order_lines
WHERE "PurchaseOrderId" IN (
    SELECT "Id" FROM purchase_orders
    WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
);
DELETE FROM purchase_orders
WHERE "WarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%');
DELETE FROM stock_transactions
WHERE "DocumentId" IN (
    SELECT "Id" FROM inventory_documents
    WHERE "DestinationWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
       OR "SourceWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
       OR "InTransitWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
);
DELETE FROM inventory_document_line_barcodes
WHERE "InventoryDocumentLineId" IN (
    SELECT "Id" FROM inventory_document_lines
    WHERE "DocumentId" IN (
        SELECT "Id" FROM inventory_documents
        WHERE "DestinationWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
           OR "SourceWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
           OR "InTransitWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
    )
);
DELETE FROM inventory_document_lines
WHERE "DocumentId" IN (
    SELECT "Id" FROM inventory_documents
    WHERE "DestinationWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
       OR "SourceWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
       OR "InTransitWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
);
DELETE FROM inventory_documents
WHERE "DestinationWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
   OR "SourceWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%')
   OR "InTransitWarehouseId" IN (SELECT "Id" FROM warehouses WHERE "Code" LIKE 'SEED-%');
DELETE FROM inventory_documents WHERE "DocumentNo" LIKE 'SEED-%';
DELETE FROM transfer_policies WHERE "Id"::text LIKE '90000000-%';
DELETE FROM markdown_policies
WHERE "BrandId" IN (SELECT "Id" FROM brands WHERE "Code" LIKE 'SEED-%');
DELETE FROM product_variants WHERE "Sku" LIKE 'SEED-%';
DELETE FROM products WHERE "ProductCode" LIKE 'SEED-%';
DELETE FROM warehouses WHERE "Code" LIKE 'SEED-%';
DELETE FROM suppliers WHERE "Code" LIKE 'SEED-%';
DELETE FROM brands WHERE "Code" LIKE 'SEED-%';

-- ---------------------------------------------------------------------------
-- Brands (:brand_count)
-- ---------------------------------------------------------------------------
INSERT INTO brands ("Id", "Code", "Name", "Status", "CreatedAt", "UpdatedAt")
SELECT
    ('10000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    'SEED-BRD-' || lpad(g::text, 5, '0'),
    'Seed Brand ' || g,
    1,
    NOW() AT TIME ZONE 'UTC',
    NOW() AT TIME ZONE 'UTC'
FROM generate_series(1, :brand_count) AS g;

-- ---------------------------------------------------------------------------
-- Products (:seed_count) — distributed across :brand_count brands
-- ---------------------------------------------------------------------------
INSERT INTO products ("Id", "ProductCode", "Name", "Brand", "BrandId", "Category", "Status", "CreatedAt", "UpdatedAt")
SELECT
    ('20000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    'SEED-PRD-' || lpad(g::text, 5, '0'),
    'Seed Product ' || g,
    'Seed Brand ' || (((g - 1) % :brand_count) + 1),
    ('10000000-0000-4000-8000-' || lpad((((g - 1) % :brand_count) + 1)::text, 12, '0'))::uuid,
    CASE (g % 5)
        WHEN 0 THEN 'Apparel'
        WHEN 1 THEN 'Footwear'
        WHEN 2 THEN 'Accessories'
        WHEN 3 THEN 'Bags'
        ELSE 'Sportswear'
    END,
    1,
    NOW() AT TIME ZONE 'UTC',
    NOW() AT TIME ZONE 'UTC'
FROM generate_series(1, :seed_count) AS g;

-- ---------------------------------------------------------------------------
-- Product variants / SKU (:seed_count)
-- ---------------------------------------------------------------------------
INSERT INTO product_variants (
    "Id", "ProductId", "BrandId", "Sku", "Barcode", "Color", "Size", "Season", "Unit",
    "Status", "CostPrice", "SellingPrice", "CostSource", "CreatedAt", "UpdatedAt"
)
SELECT
    ('30000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('20000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('10000000-0000-4000-8000-' || lpad((((g - 1) % :brand_count) + 1)::text, 12, '0'))::uuid,
    'SEED-SKU-' || lpad(g::text, 5, '0'),
    'SEED-BC-' || lpad(g::text, 5, '0'),
    (ARRAY['Black', 'White', 'Navy', 'Red', 'Grey'])[1 + (g % 5)],
    (ARRAY['S', 'M', 'L', 'XL', '36', '38', '40'])[1 + (g % 7)],
    CASE WHEN g % 2 = 0 THEN 'SS26' ELSE 'FW26' END,
    'pcs',
    1,
    (50000 + (g * 137) % 200000)::numeric(18, 4),
    (120000 + (g * 251) % 500000)::numeric(18, 4),
    1,
    NOW() AT TIME ZONE 'UTC',
    NOW() AT TIME ZONE 'UTC'
FROM generate_series(1, :seed_count) AS g;

-- ---------------------------------------------------------------------------
-- Warehouses (:seed_count) — mix DC / Store
-- ---------------------------------------------------------------------------
INSERT INTO warehouses (
    "Id", "Code", "Name", "Type", "ParentWarehouseId", "Status", "BrandId", "RegionCode",
    "FulfillmentPriority", "AddressLine", "Ward", "District", "Province", "PostalCode",
    "Phone", "ContactName", "FullAddress", "CreatedAt", "UpdatedAt"
)
SELECT
    ('40000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    'SEED-WH-' || lpad(g::text, 5, '0'),
    'Seed Warehouse ' || g,
    CASE WHEN g % 3 = 0 THEN 1 ELSE 2 END,
    NULL,
    1,
    ('10000000-0000-4000-8000-' || lpad((((g - 1) % :brand_count) + 1)::text, 12, '0'))::uuid,
    CASE (g % 4)
        WHEN 0 THEN 'HCM'
        WHEN 1 THEN 'HN'
        WHEN 2 THEN 'DN'
        ELSE 'CT'
    END,
    (g % 10) + 1,
    (g * 10) || ' Seed Street',
    'Ward ' || ((g % 20) + 1),
    'District ' || ((g % 15) + 1),
    CASE (g % 4)
        WHEN 0 THEN 'Ho Chi Minh'
        WHEN 1 THEN 'Ha Noi'
        WHEN 2 THEN 'Da Nang'
        ELSE 'Can Tho'
    END,
    lpad((70000 + g)::text, 5, '0'),
    '090' || lpad((1000000 + g)::text, 7, '0'),
    'Contact ' || g,
    (g * 10) || ' Seed Street, Ward ' || ((g % 20) + 1) || ', District ' || ((g % 15) + 1),
    NOW() AT TIME ZONE 'UTC',
    NOW() AT TIME ZONE 'UTC'
FROM generate_series(1, :seed_count) AS g;

-- ---------------------------------------------------------------------------
-- Suppliers (:seed_count)
-- ---------------------------------------------------------------------------
INSERT INTO suppliers (
    "Id", "Code", "Name", "ContactName", "Phone", "Email", "Address", "Status", "CreatedAt", "UpdatedAt"
)
SELECT
    ('50000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    'SEED-SUP-' || lpad(g::text, 5, '0'),
    'Seed Supplier ' || g,
    'Supplier Contact ' || g,
    '028' || lpad((1000000 + g)::text, 7, '0'),
    'supplier' || g || '@seed.local',
    'Industrial Zone ' || g || ', Vietnam',
    1,
    NOW() AT TIME ZONE 'UTC',
    NOW() AT TIME ZONE 'UTC'
FROM generate_series(1, :seed_count) AS g;

-- ---------------------------------------------------------------------------
-- Transfer policies (:brand_count) — cross-brand pairs
-- ---------------------------------------------------------------------------
INSERT INTO transfer_policies ("Id", "SourceBrandId", "DestinationBrandId", "AllowCrossBrand", "IsActive", "Note")
SELECT
    ('90000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('10000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('10000000-0000-4000-8000-' || lpad(((g % :brand_count) + 1)::text, 12, '0'))::uuid,
    TRUE,
    TRUE,
    'SEED-POL-' || g
FROM generate_series(1, :brand_count) AS g;

-- ---------------------------------------------------------------------------
-- Inventory documents — Stock In, Approved (:seed_count)
-- ---------------------------------------------------------------------------
INSERT INTO inventory_documents (
    "Id", "DocumentNo", "DocumentType", "SourceWarehouseId", "DestinationWarehouseId",
    "Status", "DocumentDate", "ReferenceNo", "SourceSystem", "Note",
    "CreatedBy", "CreatedAt", "ApprovedBy", "ApprovedAt",
    "TransferLifecycleStatus", "InTransitWarehouseId", "ShippedAt", "ReceivedAt"
)
SELECT
    ('60000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    'SEED-DOC-' || lpad(g::text, 5, '0'),
    1,
    NULL,
    ('40000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    3,
    (NOW() AT TIME ZONE 'UTC') - ((g % 30) || ' days')::interval,
    'SEED-REF-' || g,
    'SEED',
    'Seed stock-in document',
    'admin@stockledger.local',
    NOW() AT TIME ZONE 'UTC',
    'admin@stockledger.local',
    NOW() AT TIME ZONE 'UTC',
    0,
    NULL,
    NULL,
    NULL
FROM generate_series(1, :seed_count) AS g;

-- ---------------------------------------------------------------------------
-- Inventory document lines (:seed_count)
-- ---------------------------------------------------------------------------
INSERT INTO inventory_document_lines ("Id", "DocumentId", "ProductVariantId", "Quantity", "UnitCost", "Note")
SELECT
    ('61000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('60000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('30000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    (10 + (g % 90))::numeric(18, 4),
    (50000 + (g * 137) % 200000)::numeric(18, 4),
    'SEED line'
FROM generate_series(1, :seed_count) AS g;

-- ---------------------------------------------------------------------------
-- Stock transactions (:seed_count)
-- ---------------------------------------------------------------------------
INSERT INTO stock_transactions (
    "Id", "TransactionNo", "DocumentId", "DocumentLineId", "ProductVariantId", "WarehouseId",
    "TransactionType", "QuantityDelta", "BeforeQuantity", "AfterQuantity", "UnitCost",
    "TransactionDate", "CreatedBy", "CreatedAt"
)
SELECT
    ('70000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    'SEED-TXN-' || lpad(g::text, 5, '0'),
    ('60000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('61000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('30000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('40000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    1,
    (10 + (g % 90))::numeric(18, 4),
    0::numeric(18, 4),
    (10 + (g % 90))::numeric(18, 4),
    (50000 + (g * 137) % 200000)::numeric(18, 4),
    (NOW() AT TIME ZONE 'UTC') - ((g % 30) || ' days')::interval,
    'admin@stockledger.local',
    NOW() AT TIME ZONE 'UTC'
FROM generate_series(1, :seed_count) AS g;

-- ---------------------------------------------------------------------------
-- Current stocks (:seed_count) — SKU i @ warehouse i
-- ---------------------------------------------------------------------------
INSERT INTO current_stocks (
    "Id", "ProductVariantId", "WarehouseId",
    "QuantityOnHand", "QuantityReserved", "QuantityAvailable",
    "LastTransactionId", "LastUpdatedAt"
)
SELECT
    ('80000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('30000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    ('40000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    (10 + (g % 90))::numeric(18, 4),
    (g % 5)::numeric(18, 4),
    GREATEST((10 + (g % 90) - (g % 5))::numeric(18, 4), 0),
    ('70000000-0000-4000-8000-' || lpad(g::text, 12, '0'))::uuid,
    NOW() AT TIME ZONE 'UTC'
FROM generate_series(1, :seed_count) AS g;

COMMIT;

-- Summary
SELECT 'brands' AS entity, COUNT(*) AS rows FROM brands WHERE "Code" LIKE 'SEED-%'
UNION ALL SELECT 'products', COUNT(*) FROM products WHERE "ProductCode" LIKE 'SEED-%'
UNION ALL SELECT 'product_variants', COUNT(*) FROM product_variants WHERE "Sku" LIKE 'SEED-%'
UNION ALL SELECT 'warehouses', COUNT(*) FROM warehouses WHERE "Code" LIKE 'SEED-%'
UNION ALL SELECT 'suppliers', COUNT(*) FROM suppliers WHERE "Code" LIKE 'SEED-%'
UNION ALL SELECT 'transfer_policies', COUNT(*) FROM transfer_policies WHERE "Id"::text LIKE '90000000-%'
UNION ALL SELECT 'inventory_documents', COUNT(*) FROM inventory_documents WHERE "DocumentNo" LIKE 'SEED-%'
UNION ALL SELECT 'inventory_document_lines', COUNT(*) FROM inventory_document_lines WHERE "Note" = 'SEED line'
UNION ALL SELECT 'stock_transactions', COUNT(*) FROM stock_transactions WHERE "TransactionNo" LIKE 'SEED-%'
UNION ALL SELECT 'current_stocks', COUNT(*) FROM current_stocks WHERE "Id"::text LIKE '80000000-%'
ORDER BY entity;
