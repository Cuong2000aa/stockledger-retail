CREATE TABLE app_users (
    "Id" uuid NOT NULL,
    "Email" character varying(200) NOT NULL,
    "DisplayName" character varying(200) NOT NULL,
    "PasswordHash" character varying(500),
    "IsActive" boolean NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_app_users" PRIMARY KEY ("Id")
);


CREATE TABLE background_job_runs (
    "Id" uuid NOT NULL,
    "JobKey" character varying(100) NOT NULL,
    "TriggeredBy" character varying(100) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Message" character varying(2000),
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    "DurationMs" bigint,
    CONSTRAINT "PK_background_job_runs" PRIMARY KEY ("Id")
);


CREATE TABLE background_job_settings (
    "Id" uuid NOT NULL,
    "JobKey" character varying(100) NOT NULL,
    "DisplayName" character varying(200) NOT NULL,
    "Description" character varying(500),
    "IsEnabled" boolean NOT NULL,
    "IntervalMinutes" integer NOT NULL,
    "LastStatus" character varying(30) NOT NULL,
    "LastMessage" character varying(2000),
    "LastRunStartedAtUtc" timestamp with time zone,
    "LastRunCompletedAtUtc" timestamp with time zone,
    "NextRunAtUtc" timestamp with time zone,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_background_job_settings" PRIMARY KEY ("Id")
);


CREATE TABLE brands (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Status" integer NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_brands" PRIMARY KEY ("Id")
);


CREATE TABLE insight_action_logs (
    "Id" uuid NOT NULL,
    "InsightKind" character varying(50) NOT NULL,
    "ActionCode" character varying(100) NOT NULL,
    "ActionStatus" integer NOT NULL,
    "ProductVariantId" uuid,
    "WarehouseId" uuid,
    "SourceWarehouseId" uuid,
    "DestinationWarehouseId" uuid,
    "PayloadJson" text,
    "ResultEntityId" uuid,
    "ResultEntityType" character varying(50),
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IpAddress" character varying(50),
    CONSTRAINT "PK_insight_action_logs" PRIMARY KEY ("Id")
);


CREATE TABLE insight_snapshots (
    "Id" uuid NOT NULL,
    "SnapshotKey" character varying(500) NOT NULL,
    "InsightKind" character varying(50) NOT NULL,
    "PayloadJson" text NOT NULL,
    "GeneratedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_insight_snapshots" PRIMARY KEY ("Id")
);


CREATE TABLE inventory_daily_rollups (
    "Id" uuid NOT NULL,
    "SnapshotDate" date NOT NULL,
    "BrandId" uuid,
    "WarehouseId" uuid,
    "RegionCode" character varying(20),
    "SkuCount" integer NOT NULL,
    "TotalOnHand" numeric(18,4) NOT NULL,
    "TotalAvailable" numeric(18,4) NOT NULL,
    "TotalInventoryValue" numeric(18,4) NOT NULL,
    "OutboundQty30d" numeric(18,4) NOT NULL,
    "GeneratedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_inventory_daily_rollups" PRIMARY KEY ("Id")
);


CREATE TABLE permission_groups (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_permission_groups" PRIMARY KEY ("Id")
);


CREATE TABLE permissions (
    "Id" uuid NOT NULL,
    "Code" character varying(100) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Category" character varying(100),
    CONSTRAINT "PK_permissions" PRIMARY KEY ("Id")
);


CREATE TABLE suppliers (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "ContactName" character varying(100),
    "Phone" character varying(50),
    "Email" character varying(200),
    "Address" character varying(500),
    "Status" integer NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_suppliers" PRIMARY KEY ("Id")
);


CREATE TABLE transaction_logs (
    "Id" uuid NOT NULL,
    "EntityName" character varying(100) NOT NULL,
    "EntityId" uuid NOT NULL,
    "Action" integer NOT NULL,
    "OldValue" text,
    "NewValue" text,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IpAddress" character varying(50),
    CONSTRAINT "PK_transaction_logs" PRIMARY KEY ("Id")
);


CREATE TABLE app_user_refresh_tokens (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TokenHash" character varying(128) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone,
    CONSTRAINT "PK_app_user_refresh_tokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_app_user_refresh_tokens_app_users_UserId" FOREIGN KEY ("UserId") REFERENCES app_users ("Id") ON DELETE CASCADE
);


CREATE TABLE teams (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "LeaderUserId" uuid NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_teams" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_teams_app_users_LeaderUserId" FOREIGN KEY ("LeaderUserId") REFERENCES app_users ("Id") ON DELETE RESTRICT
);


CREATE TABLE markdown_policies (
    "Id" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "RegionCode" character varying(50),
    "WarehouseType" integer,
    "LookbackDays" integer NOT NULL,
    "MinDaysWithoutOutbound" integer NOT NULL,
    "MinOnHand" numeric(18,4) NOT NULL,
    "MinInventoryValueAtCost" numeric(18,4),
    "MinGrossMarginPercent" numeric(9,4) NOT NULL,
    "MaxMarkdownPercent" numeric(9,4) NOT NULL,
    "AllowBelowCost" boolean NOT NULL,
    "RequireApprovalAbovePercent" numeric(9,4),
    "SlowSellThroughThreshold" numeric(9,4) NOT NULL,
    "TiersJson" jsonb NOT NULL,
    "IsActive" boolean NOT NULL,
    "Note" character varying(500),
    CONSTRAINT "PK_markdown_policies" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_markdown_policies_brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES brands ("Id") ON DELETE RESTRICT
);


CREATE TABLE products (
    "Id" uuid NOT NULL,
    "ProductCode" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Brand" character varying(100),
    "BrandId" uuid,
    "Category" character varying(100),
    "Status" integer NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_products" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_products_brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES brands ("Id") ON DELETE RESTRICT
);


CREATE TABLE transfer_policies (
    "Id" uuid NOT NULL,
    "SourceBrandId" uuid,
    "DestinationBrandId" uuid,
    "AllowCrossBrand" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "Note" character varying(500),
    CONSTRAINT "PK_transfer_policies" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_transfer_policies_brands_DestinationBrandId" FOREIGN KEY ("DestinationBrandId") REFERENCES brands ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_transfer_policies_brands_SourceBrandId" FOREIGN KEY ("SourceBrandId") REFERENCES brands ("Id") ON DELETE RESTRICT
);


CREATE TABLE warehouses (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Type" integer NOT NULL,
    "ParentWarehouseId" uuid,
    "Status" integer NOT NULL,
    "BrandId" uuid,
    "RegionCode" character varying(20),
    "FulfillmentPriority" integer NOT NULL,
    "AddressLine" character varying(300),
    "Ward" character varying(100),
    "District" character varying(100),
    "Province" character varying(100),
    "PostalCode" character varying(20),
    "Phone" character varying(30),
    "ContactName" character varying(150),
    "FullAddress" character varying(1000),
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_warehouses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_warehouses_brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES brands ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_warehouses_warehouses_ParentWarehouseId" FOREIGN KEY ("ParentWarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE user_group_assignments (
    "UserId" uuid NOT NULL,
    "GroupId" uuid NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_user_group_assignments" PRIMARY KEY ("UserId", "GroupId"),
    CONSTRAINT "FK_user_group_assignments_app_users_UserId" FOREIGN KEY ("UserId") REFERENCES app_users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_user_group_assignments_permission_groups_GroupId" FOREIGN KEY ("GroupId") REFERENCES permission_groups ("Id") ON DELETE CASCADE
);


CREATE TABLE group_permissions (
    "GroupId" uuid NOT NULL,
    "PermissionId" uuid NOT NULL,
    CONSTRAINT "PK_group_permissions" PRIMARY KEY ("GroupId", "PermissionId"),
    CONSTRAINT "FK_group_permissions_permission_groups_GroupId" FOREIGN KEY ("GroupId") REFERENCES permission_groups ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_group_permissions_permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES permissions ("Id") ON DELETE CASCADE
);


CREATE TABLE team_members (
    "TeamId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "JoinedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_team_members" PRIMARY KEY ("TeamId", "UserId"),
    CONSTRAINT "FK_team_members_app_users_UserId" FOREIGN KEY ("UserId") REFERENCES app_users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_team_members_teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES teams ("Id") ON DELETE CASCADE
);


CREATE TABLE product_variants (
    "Id" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "BrandId" uuid,
    "Sku" character varying(50) NOT NULL,
    "Barcode" character varying(50),
    "Color" character varying(50),
    "Size" character varying(50),
    "Season" character varying(50),
    "Unit" character varying(20),
    "Status" integer NOT NULL,
    "CostPrice" numeric(18,4),
    "SellingPrice" numeric(18,4),
    "CostSource" integer,
    "CurrentCostPrice" numeric(18,4),
    "CurrentSellingPrice" numeric(18,4),
    "CurrentSellingPriceBeforeVat" numeric(18,4),
    "CurrentSellingPriceAfterVat" numeric(18,4),
    "VatRate" numeric(5,2),
    "CurrentCostSource" integer,
    "CurrentCostEffectiveFrom" timestamp with time zone,
    "CurrentPriceEffectiveFrom" timestamp with time zone,
    "TrackLotExpiry" boolean NOT NULL,
    "IsBarcode" boolean NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_product_variants" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_product_variants_cost_price_non_negative" CHECK ("CostPrice" IS NULL OR "CostPrice" >= 0),
    CONSTRAINT "CK_product_variants_current_cost_price_non_negative" CHECK ("CurrentCostPrice" IS NULL OR "CurrentCostPrice" >= 0),
    CONSTRAINT "CK_product_variants_current_selling_price_non_negative" CHECK ("CurrentSellingPrice" IS NULL OR "CurrentSellingPrice" >= 0),
    CONSTRAINT "CK_product_variants_selling_price_non_negative" CHECK ("SellingPrice" IS NULL OR "SellingPrice" >= 0),
    CONSTRAINT "CK_product_variants_vat_rate_non_negative" CHECK ("VatRate" IS NULL OR "VatRate" >= 0),
    CONSTRAINT "FK_product_variants_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
);


CREATE TABLE inventory_documents (
    "Id" uuid NOT NULL,
    "DocumentNo" character varying(50) NOT NULL,
    "DocumentType" integer NOT NULL,
    "SourceWarehouseId" uuid,
    "DestinationWarehouseId" uuid,
    "Status" integer NOT NULL,
    "DocumentDate" timestamp with time zone NOT NULL,
    "ReferenceNo" character varying(100),
    "SourceSystem" character varying(50),
    "Note" character varying(500),
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ApprovedBy" character varying(100),
    "ApprovedAt" timestamp with time zone,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone,
    "SubmittedAt" timestamp with time zone,
    "SubmittedBy" text,
    "RequiredApprovalSteps" integer NOT NULL,
    "CompletedApprovalSteps" integer NOT NULL,
    "FirstApprovedBy" text,
    "FirstApprovedAt" timestamp with time zone,
    "TransferLifecycleStatus" integer NOT NULL,
    "InTransitWarehouseId" uuid,
    "ShippedAt" timestamp with time zone,
    "ReceivedAt" timestamp with time zone,
    CONSTRAINT "PK_inventory_documents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_documents_warehouses_DestinationWarehouseId" FOREIGN KEY ("DestinationWarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_inventory_documents_warehouses_InTransitWarehouseId" FOREIGN KEY ("InTransitWarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_inventory_documents_warehouses_SourceWarehouseId" FOREIGN KEY ("SourceWarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE purchase_orders (
    "Id" uuid NOT NULL,
    "PoNo" character varying(50) NOT NULL,
    "SupplierId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "OrderDate" timestamp with time zone NOT NULL,
    "ExpectedDate" timestamp with time zone,
    "ReferenceNo" character varying(100),
    "Note" character varying(500),
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "SubmittedAt" timestamp with time zone,
    "RequiredApprovalSteps" integer NOT NULL,
    "CompletedApprovalSteps" integer NOT NULL,
    "FirstApprovedBy" text,
    "FirstApprovedAt" timestamp with time zone,
    "ApprovedBy" text,
    "ApprovedAt" timestamp with time zone,
    "CancelledAt" timestamp with time zone,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_purchase_orders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_purchase_orders_suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES suppliers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_purchase_orders_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE stock_reservations (
    "Id" uuid NOT NULL,
    "ReservationNo" character varying(50) NOT NULL,
    "SourceSystem" character varying(50) NOT NULL,
    "ReferenceType" integer NOT NULL,
    "ReferenceKey" character varying(100) NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CommittedAt" timestamp with time zone,
    "ReleasedAt" timestamp with time zone,
    CONSTRAINT "PK_stock_reservations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_stock_reservations_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE user_warehouse_assignments (
    "UserId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "IsPrimary" boolean NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_user_warehouse_assignments" PRIMARY KEY ("UserId", "WarehouseId"),
    CONSTRAINT "FK_user_warehouse_assignments_app_users_UserId" FOREIGN KEY ("UserId") REFERENCES app_users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_user_warehouse_assignments_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE CASCADE
);


CREATE TABLE inventory_valuation_snapshots (
    "Id" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "QuantityOnHand" numeric(18,4) NOT NULL,
    "QuantityReserved" numeric(18,4) NOT NULL,
    "QuantityAvailable" numeric(18,4) NOT NULL,
    "AverageCost" numeric(18,4),
    "InventoryValue" numeric(18,4) NOT NULL,
    "SnapshotDate" timestamp with time zone NOT NULL,
    "ValuationMethod" integer NOT NULL,
    "Currency" character varying(10) NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_inventory_valuation_snapshots" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_inventory_valuation_snapshots_available_non_negative" CHECK ("QuantityAvailable" >= 0),
    CONSTRAINT "CK_inventory_valuation_snapshots_on_hand_non_negative" CHECK ("QuantityOnHand" >= 0),
    CONSTRAINT "CK_inventory_valuation_snapshots_reserved_non_negative" CHECK ("QuantityReserved" >= 0),
    CONSTRAINT "CK_inventory_valuation_snapshots_value_non_negative" CHECK ("InventoryValue" >= 0),
    CONSTRAINT "FK_inventory_valuation_snapshots_product_variants_ProductVaria~" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_inventory_valuation_snapshots_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE product_cost_histories (
    "Id" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "CostPrice" numeric(18,4) NOT NULL,
    "CostSource" integer NOT NULL,
    "ValuationMethod" integer NOT NULL,
    "Currency" character varying(10) NOT NULL,
    "ReferenceType" character varying(50),
    "ReferenceId" uuid,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone,
    "IsCurrent" boolean NOT NULL,
    CONSTRAINT "PK_product_cost_histories" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_product_cost_history_cost_price_non_negative" CHECK ("CostPrice" >= 0),
    CONSTRAINT "FK_product_cost_histories_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT
);


CREATE TABLE product_prices (
    "Id" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "PriceType" integer NOT NULL,
    "PriceBeforeVat" numeric(18,4) NOT NULL,
    "VatRate" numeric(5,2) NOT NULL,
    "PriceAfterVat" numeric(18,4) NOT NULL,
    "Currency" character varying(10) NOT NULL,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone,
    "IsCurrent" boolean NOT NULL,
    "ChannelCode" character varying(50),
    "ReferenceType" character varying(50),
    "ReferenceId" uuid,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_product_prices" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_product_prices_after_vat_non_negative" CHECK ("PriceAfterVat" >= 0),
    CONSTRAINT "CK_product_prices_before_vat_non_negative" CHECK ("PriceBeforeVat" >= 0),
    CONSTRAINT "CK_product_prices_vat_rate_non_negative" CHECK ("VatRate" >= 0),
    CONSTRAINT "FK_product_prices_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT
);


CREATE TABLE stock_lots (
    "Id" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "LotCode" character varying(64) NOT NULL,
    "ExpiryDate" timestamp with time zone,
    "ManufacturedDate" timestamp with time zone,
    "ReceivedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_stock_lots" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_stock_lots_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE CASCADE
);


CREATE TABLE variant_unit_barcodes (
    "Id" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "Barcode" character varying(50) NOT NULL,
    "WarehouseId" uuid,
    "Status" integer NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "LastUpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_variant_unit_barcodes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_variant_unit_barcodes_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_variant_unit_barcodes_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE goods_receipts (
    "Id" uuid NOT NULL,
    "GrNo" character varying(50) NOT NULL,
    "PurchaseOrderId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "ReceiptDate" timestamp with time zone NOT NULL,
    "ReferenceNo" character varying(100),
    "Note" character varying(500),
    "InventoryDocumentId" uuid,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ApprovedBy" character varying(100),
    "ApprovedAt" timestamp with time zone,
    "UpdatedBy" character varying(100),
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_goods_receipts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_goods_receipts_inventory_documents_InventoryDocumentId" FOREIGN KEY ("InventoryDocumentId") REFERENCES inventory_documents ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_goods_receipts_purchase_orders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES purchase_orders ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_goods_receipts_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE purchase_order_lines (
    "Id" uuid NOT NULL,
    "PurchaseOrderId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "OrderedQuantity" numeric(18,4) NOT NULL,
    "ReceivedQuantity" numeric(18,4) NOT NULL,
    "UnitCost" numeric(18,4),
    "Note" character varying(500),
    CONSTRAINT "PK_purchase_order_lines" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_purchase_order_lines_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_purchase_order_lines_purchase_orders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES purchase_orders ("Id") ON DELETE CASCADE
);


CREATE TABLE stock_reservation_lines (
    "Id" uuid NOT NULL,
    "StockReservationId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    CONSTRAINT "PK_stock_reservation_lines" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_stock_reservation_lines_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_stock_reservation_lines_stock_reservations_StockReservation~" FOREIGN KEY ("StockReservationId") REFERENCES stock_reservations ("Id") ON DELETE CASCADE
);


CREATE TABLE inventory_document_lines (
    "Id" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "UnitCost" numeric(18,4),
    "StockLotId" uuid,
    "LotCode" character varying(64),
    "ExpiryDate" timestamp with time zone,
    "Note" character varying(500),
    CONSTRAINT "PK_inventory_document_lines" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_document_lines_inventory_documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES inventory_documents ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_inventory_document_lines_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_inventory_document_lines_stock_lots_StockLotId" FOREIGN KEY ("StockLotId") REFERENCES stock_lots ("Id")
);


CREATE TABLE lot_stocks (
    "Id" uuid NOT NULL,
    "StockLotId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "QuantityOnHand" numeric(18,4) NOT NULL,
    "LastUpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_lot_stocks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_lot_stocks_stock_lots_StockLotId" FOREIGN KEY ("StockLotId") REFERENCES stock_lots ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_lot_stocks_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE CASCADE
);


CREATE TABLE goods_receipt_lines (
    "Id" uuid NOT NULL,
    "GoodsReceiptId" uuid NOT NULL,
    "PurchaseOrderLineId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "ReceivedQuantity" numeric(18,4) NOT NULL,
    "UnitCost" numeric(18,4),
    "LotCode" character varying(64),
    "ExpiryDate" timestamp with time zone,
    "Note" character varying(500),
    CONSTRAINT "PK_goods_receipt_lines" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_goods_receipt_lines_goods_receipts_GoodsReceiptId" FOREIGN KEY ("GoodsReceiptId") REFERENCES goods_receipts ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_goods_receipt_lines_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_goods_receipt_lines_purchase_order_lines_PurchaseOrderLineId" FOREIGN KEY ("PurchaseOrderLineId") REFERENCES purchase_order_lines ("Id") ON DELETE RESTRICT
);


CREATE TABLE purchase_order_line_barcodes (
    "Id" uuid NOT NULL,
    "PurchaseOrderLineId" uuid NOT NULL,
    "Barcode" character varying(50) NOT NULL,
    CONSTRAINT "PK_purchase_order_line_barcodes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_purchase_order_line_barcodes_purchase_order_lines_PurchaseO~" FOREIGN KEY ("PurchaseOrderLineId") REFERENCES purchase_order_lines ("Id") ON DELETE CASCADE
);


CREATE TABLE inventory_document_line_barcodes (
    "Id" uuid NOT NULL,
    "InventoryDocumentLineId" uuid NOT NULL,
    "Barcode" character varying(50) NOT NULL,
    CONSTRAINT "PK_inventory_document_line_barcodes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_document_line_barcodes_inventory_document_lines_I~" FOREIGN KEY ("InventoryDocumentLineId") REFERENCES inventory_document_lines ("Id") ON DELETE CASCADE
);


CREATE TABLE stock_transactions (
    "Id" uuid NOT NULL,
    "TransactionNo" character varying(50) NOT NULL,
    "DocumentId" uuid NOT NULL,
    "DocumentLineId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "TransactionType" integer NOT NULL,
    "QuantityDelta" numeric(18,4) NOT NULL,
    "BeforeQuantity" numeric(18,4) NOT NULL,
    "AfterQuantity" numeric(18,4) NOT NULL,
    "UnitCost" numeric(18,4),
    "TransactionDate" timestamp with time zone NOT NULL,
    "DocumentNo" character varying(50) NOT NULL,
    "SourceSystem" character varying(50),
    "ReferenceNo" character varying(100),
    "CounterpartWarehouseId" uuid,
    "CreatedBy" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_stock_transactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_stock_transactions_inventory_document_lines_DocumentLineId" FOREIGN KEY ("DocumentLineId") REFERENCES inventory_document_lines ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_stock_transactions_inventory_documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES inventory_documents ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_stock_transactions_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_stock_transactions_warehouses_CounterpartWarehouseId" FOREIGN KEY ("CounterpartWarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_stock_transactions_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE goods_receipt_line_barcodes (
    "Id" uuid NOT NULL,
    "GoodsReceiptLineId" uuid NOT NULL,
    "Barcode" character varying(50) NOT NULL,
    CONSTRAINT "PK_goods_receipt_line_barcodes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_goods_receipt_line_barcodes_goods_receipt_lines_GoodsReceip~" FOREIGN KEY ("GoodsReceiptLineId") REFERENCES goods_receipt_lines ("Id") ON DELETE CASCADE
);


CREATE TABLE current_stocks (
    "Id" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "QuantityOnHand" numeric(18,4) NOT NULL,
    "QuantityReserved" numeric(18,4) NOT NULL,
    "QuantityAvailable" numeric(18,4) NOT NULL,
    "LastTransactionId" uuid,
    "LastUpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_current_stocks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_current_stocks_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_current_stocks_stock_transactions_LastTransactionId" FOREIGN KEY ("LastTransactionId") REFERENCES stock_transactions ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_current_stocks_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES warehouses ("Id") ON DELETE RESTRICT
);


CREATE TABLE stock_transaction_barcodes (
    "Id" uuid NOT NULL,
    "StockTransactionId" uuid NOT NULL,
    "Barcode" character varying(100) NOT NULL,
    CONSTRAINT "PK_stock_transaction_barcodes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_stock_transaction_barcodes_stock_transactions_StockTransact~" FOREIGN KEY ("StockTransactionId") REFERENCES stock_transactions ("Id") ON DELETE CASCADE
);


CREATE UNIQUE INDEX "IX_app_user_refresh_tokens_TokenHash" ON app_user_refresh_tokens ("TokenHash");


CREATE INDEX "IX_app_user_refresh_tokens_UserId_RevokedAt" ON app_user_refresh_tokens ("UserId", "RevokedAt");


CREATE UNIQUE INDEX "IX_app_users_Email" ON app_users ("Email");


CREATE INDEX "IX_background_job_runs_JobKey_StartedAtUtc" ON background_job_runs ("JobKey", "StartedAtUtc");


CREATE UNIQUE INDEX "IX_background_job_settings_JobKey" ON background_job_settings ("JobKey");


CREATE UNIQUE INDEX "IX_brands_Code" ON brands ("Code");


CREATE INDEX "IX_current_stocks_LastTransactionId" ON current_stocks ("LastTransactionId");


CREATE INDEX "IX_current_stocks_on_hand_pairs" ON current_stocks ("WarehouseId", "ProductVariantId") WHERE "QuantityOnHand" > 0;


CREATE UNIQUE INDEX "IX_current_stocks_ProductVariantId_WarehouseId" ON current_stocks ("ProductVariantId", "WarehouseId");


CREATE INDEX "IX_goods_receipt_line_barcodes_Barcode" ON goods_receipt_line_barcodes ("Barcode");


CREATE INDEX "IX_goods_receipt_line_barcodes_GoodsReceiptLineId" ON goods_receipt_line_barcodes ("GoodsReceiptLineId");


CREATE INDEX "IX_goods_receipt_lines_GoodsReceiptId" ON goods_receipt_lines ("GoodsReceiptId");


CREATE INDEX "IX_goods_receipt_lines_ProductVariantId" ON goods_receipt_lines ("ProductVariantId");


CREATE INDEX "IX_goods_receipt_lines_PurchaseOrderLineId" ON goods_receipt_lines ("PurchaseOrderLineId");


CREATE UNIQUE INDEX "IX_goods_receipts_GrNo" ON goods_receipts ("GrNo");


CREATE INDEX "IX_goods_receipts_InventoryDocumentId" ON goods_receipts ("InventoryDocumentId");


CREATE INDEX "IX_goods_receipts_PurchaseOrderId" ON goods_receipts ("PurchaseOrderId");


CREATE INDEX "IX_goods_receipts_Status" ON goods_receipts ("Status");


CREATE INDEX "IX_goods_receipts_Status_ReceiptDate_CreatedAt" ON goods_receipts ("Status", "ReceiptDate" DESC, "CreatedAt" DESC);


CREATE INDEX "IX_goods_receipts_WarehouseId" ON goods_receipts ("WarehouseId");


CREATE INDEX "IX_goods_receipts_WarehouseId_ReceiptDate_CreatedAt" ON goods_receipts ("WarehouseId", "ReceiptDate" DESC, "CreatedAt" DESC);


CREATE INDEX "IX_group_permissions_PermissionId" ON group_permissions ("PermissionId");


CREATE INDEX "IX_insight_action_logs_ActionStatus" ON insight_action_logs ("ActionStatus");


CREATE INDEX "IX_insight_action_logs_CreatedAt" ON insight_action_logs ("CreatedAt");


CREATE INDEX "IX_insight_action_logs_InsightKind" ON insight_action_logs ("InsightKind");


CREATE INDEX "IX_insight_action_logs_ProductVariantId_WarehouseId" ON insight_action_logs ("ProductVariantId", "WarehouseId");


CREATE INDEX "IX_insight_snapshots_InsightKind_GeneratedAtUtc" ON insight_snapshots ("InsightKind", "GeneratedAtUtc");


CREATE UNIQUE INDEX "IX_insight_snapshots_SnapshotKey" ON insight_snapshots ("SnapshotKey");


CREATE INDEX "IX_inventory_daily_rollups_BrandId_SnapshotDate" ON inventory_daily_rollups ("BrandId", "SnapshotDate");


CREATE UNIQUE INDEX "IX_inventory_daily_rollups_SnapshotDate_BrandId_WarehouseId_Re~" ON inventory_daily_rollups ("SnapshotDate", "BrandId", "WarehouseId", "RegionCode");


CREATE INDEX "IX_inventory_document_line_barcodes_Barcode" ON inventory_document_line_barcodes ("Barcode");


CREATE INDEX "IX_inventory_document_line_barcodes_InventoryDocumentLineId" ON inventory_document_line_barcodes ("InventoryDocumentLineId");


CREATE INDEX "IX_inventory_document_lines_DocumentId" ON inventory_document_lines ("DocumentId");


CREATE INDEX "IX_inventory_document_lines_ProductVariantId" ON inventory_document_lines ("ProductVariantId");


CREATE INDEX "IX_inventory_document_lines_StockLotId" ON inventory_document_lines ("StockLotId");


CREATE INDEX "IX_inventory_documents_DestinationWarehouseId" ON inventory_documents ("DestinationWarehouseId");


CREATE UNIQUE INDEX "IX_inventory_documents_DocumentNo" ON inventory_documents ("DocumentNo");


CREATE INDEX "IX_inventory_documents_InTransitWarehouseId" ON inventory_documents ("InTransitWarehouseId");


CREATE UNIQUE INDEX "IX_inventory_documents_SourceSystem_ReferenceNo_DocumentType" ON inventory_documents ("SourceSystem", "ReferenceNo", "DocumentType") WHERE "ReferenceNo" IS NOT NULL AND "SourceSystem" IS NOT NULL;


CREATE INDEX "IX_inventory_documents_SourceWarehouseId" ON inventory_documents ("SourceWarehouseId");


CREATE UNIQUE INDEX "IX_inventory_valuation_snapshots_ProductVariantId_WarehouseId_~" ON inventory_valuation_snapshots ("ProductVariantId", "WarehouseId", "SnapshotDate");


CREATE INDEX "IX_inventory_valuation_snapshots_WarehouseId_SnapshotDate" ON inventory_valuation_snapshots ("WarehouseId", "SnapshotDate");


CREATE UNIQUE INDEX "IX_lot_stocks_StockLotId_WarehouseId" ON lot_stocks ("StockLotId", "WarehouseId");


CREATE INDEX "IX_lot_stocks_WarehouseId" ON lot_stocks ("WarehouseId");


CREATE INDEX "IX_markdown_policies_BrandId_RegionCode_WarehouseType_IsActive" ON markdown_policies ("BrandId", "RegionCode", "WarehouseType", "IsActive");


CREATE UNIQUE INDEX "IX_permission_groups_Code" ON permission_groups ("Code");


CREATE UNIQUE INDEX "IX_permissions_Code" ON permissions ("Code");


CREATE INDEX "IX_product_cost_histories_ProductVariantId" ON product_cost_histories ("ProductVariantId");


CREATE INDEX "IX_product_cost_histories_ProductVariantId_EffectiveFrom" ON product_cost_histories ("ProductVariantId", "EffectiveFrom");


CREATE INDEX "IX_product_cost_histories_ProductVariantId_IsCurrent" ON product_cost_histories ("ProductVariantId", "IsCurrent");


CREATE INDEX "IX_product_prices_ProductVariantId" ON product_prices ("ProductVariantId");


CREATE INDEX "IX_product_prices_ProductVariantId_EffectiveFrom" ON product_prices ("ProductVariantId", "EffectiveFrom");


CREATE INDEX "IX_product_prices_ProductVariantId_IsCurrent" ON product_prices ("ProductVariantId", "IsCurrent");


CREATE UNIQUE INDEX "IX_product_variants_Barcode" ON product_variants ("Barcode");


CREATE UNIQUE INDEX "IX_product_variants_BrandId_Sku" ON product_variants ("BrandId", "Sku");


CREATE INDEX "IX_product_variants_ProductId" ON product_variants ("ProductId");


CREATE INDEX "IX_products_BrandId" ON products ("BrandId");


CREATE UNIQUE INDEX "IX_products_ProductCode" ON products ("ProductCode");


CREATE INDEX "IX_purchase_order_line_barcodes_Barcode" ON purchase_order_line_barcodes ("Barcode");


CREATE INDEX "IX_purchase_order_line_barcodes_PurchaseOrderLineId" ON purchase_order_line_barcodes ("PurchaseOrderLineId");


CREATE INDEX "IX_purchase_order_lines_ProductVariantId" ON purchase_order_lines ("ProductVariantId");


CREATE INDEX "IX_purchase_order_lines_PurchaseOrderId" ON purchase_order_lines ("PurchaseOrderId");


CREATE UNIQUE INDEX "IX_purchase_orders_PoNo" ON purchase_orders ("PoNo");


CREATE INDEX "IX_purchase_orders_Status" ON purchase_orders ("Status");


CREATE INDEX "IX_purchase_orders_Status_OrderDate_CreatedAt" ON purchase_orders ("Status", "OrderDate" DESC, "CreatedAt" DESC);


CREATE INDEX "IX_purchase_orders_SupplierId" ON purchase_orders ("SupplierId");


CREATE INDEX "IX_purchase_orders_WarehouseId" ON purchase_orders ("WarehouseId");


CREATE INDEX "IX_purchase_orders_WarehouseId_OrderDate_CreatedAt" ON purchase_orders ("WarehouseId", "OrderDate" DESC, "CreatedAt" DESC);


CREATE UNIQUE INDEX "IX_stock_lots_ProductVariantId_LotCode" ON stock_lots ("ProductVariantId", "LotCode");


CREATE INDEX "IX_stock_reservation_lines_ProductVariantId" ON stock_reservation_lines ("ProductVariantId");


CREATE UNIQUE INDEX "IX_stock_reservation_lines_StockReservationId_ProductVariantId" ON stock_reservation_lines ("StockReservationId", "ProductVariantId");


CREATE UNIQUE INDEX "IX_stock_reservations_ReservationNo" ON stock_reservations ("ReservationNo");


CREATE INDEX "IX_stock_reservations_SourceSystem_ReferenceType_ReferenceKey_~" ON stock_reservations ("SourceSystem", "ReferenceType", "ReferenceKey", "WarehouseId", "Status");


CREATE INDEX "IX_stock_reservations_WarehouseId" ON stock_reservations ("WarehouseId");


CREATE INDEX "IX_stock_transaction_barcodes_Barcode" ON stock_transaction_barcodes ("Barcode");


CREATE INDEX "IX_stock_transaction_barcodes_StockTransactionId" ON stock_transaction_barcodes ("StockTransactionId");


CREATE INDEX "IX_stock_transactions_CounterpartWarehouseId" ON stock_transactions ("CounterpartWarehouseId");


CREATE INDEX "IX_stock_transactions_DocumentId" ON stock_transactions ("DocumentId");


CREATE INDEX "IX_stock_transactions_DocumentLineId" ON stock_transactions ("DocumentLineId");


CREATE INDEX "IX_stock_transactions_DocumentNo" ON stock_transactions ("DocumentNo");


CREATE INDEX "IX_stock_transactions_outbound_pairs" ON stock_transactions ("ProductVariantId", "WarehouseId", "TransactionDate" DESC) WHERE "TransactionType" = 2;


CREATE INDEX "IX_stock_transactions_ProductVariantId" ON stock_transactions ("ProductVariantId");


CREATE INDEX "IX_stock_transactions_ProductVariantId_WarehouseId_Transaction~" ON stock_transactions ("ProductVariantId", "WarehouseId", "TransactionDate" DESC, "CreatedAt" DESC);


CREATE INDEX "IX_stock_transactions_TransactionDate" ON stock_transactions ("TransactionDate");


CREATE UNIQUE INDEX "IX_stock_transactions_TransactionNo" ON stock_transactions ("TransactionNo");


CREATE INDEX "IX_stock_transactions_WarehouseId" ON stock_transactions ("WarehouseId");


CREATE INDEX "IX_stock_transactions_WarehouseId_TransactionDate_CreatedAt" ON stock_transactions ("WarehouseId", "TransactionDate" DESC, "CreatedAt" DESC);


CREATE UNIQUE INDEX "IX_suppliers_Code" ON suppliers ("Code");


CREATE INDEX "IX_team_members_UserId" ON team_members ("UserId");


CREATE UNIQUE INDEX "IX_teams_Code" ON teams ("Code");


CREATE INDEX "IX_teams_LeaderUserId" ON teams ("LeaderUserId");


CREATE INDEX "IX_transaction_logs_Action_CreatedAt" ON transaction_logs ("Action", "CreatedAt");


CREATE INDEX "IX_transaction_logs_CreatedAt" ON transaction_logs ("CreatedAt");


CREATE INDEX "IX_transaction_logs_CreatedBy_CreatedAt" ON transaction_logs ("CreatedBy", "CreatedAt");


CREATE INDEX "IX_transaction_logs_EntityId" ON transaction_logs ("EntityId");


CREATE INDEX "IX_transaction_logs_EntityName" ON transaction_logs ("EntityName");


CREATE INDEX "IX_transaction_logs_EntityName_CreatedAt" ON transaction_logs ("EntityName", "CreatedAt");


CREATE INDEX "IX_transfer_policies_DestinationBrandId" ON transfer_policies ("DestinationBrandId");


CREATE INDEX "IX_transfer_policies_SourceBrandId_DestinationBrandId_IsActive" ON transfer_policies ("SourceBrandId", "DestinationBrandId", "IsActive");


CREATE INDEX "IX_user_group_assignments_GroupId" ON user_group_assignments ("GroupId");


CREATE INDEX "IX_user_warehouse_assignments_UserId_IsPrimary" ON user_warehouse_assignments ("UserId", "IsPrimary");


CREATE INDEX "IX_user_warehouse_assignments_WarehouseId" ON user_warehouse_assignments ("WarehouseId");


CREATE UNIQUE INDEX "IX_variant_unit_barcodes_Barcode" ON variant_unit_barcodes ("Barcode");


CREATE INDEX "IX_variant_unit_barcodes_ProductVariantId" ON variant_unit_barcodes ("ProductVariantId");


CREATE INDEX "IX_variant_unit_barcodes_ProductVariantId_WarehouseId_Status" ON variant_unit_barcodes ("ProductVariantId", "WarehouseId", "Status");


CREATE INDEX "IX_variant_unit_barcodes_WarehouseId" ON variant_unit_barcodes ("WarehouseId");


CREATE INDEX "IX_warehouses_BrandId" ON warehouses ("BrandId");


CREATE UNIQUE INDEX "IX_warehouses_Code" ON warehouses ("Code");


CREATE INDEX "IX_warehouses_ParentWarehouseId" ON warehouses ("ParentWarehouseId");


CREATE INDEX "IX_warehouses_Type_BrandId" ON warehouses ("Type", "BrandId");


