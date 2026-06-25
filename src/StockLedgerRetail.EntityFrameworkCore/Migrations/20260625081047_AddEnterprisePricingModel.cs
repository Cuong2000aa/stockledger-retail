using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterprisePricingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentCostEffectiveFrom",
                table: "product_variants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentCostPrice",
                table: "product_variants",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentCostSource",
                table: "product_variants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPriceEffectiveFrom",
                table: "product_variants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentSellingPrice",
                table: "product_variants",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentSellingPriceAfterVat",
                table: "product_variants",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentSellingPriceBeforeVat",
                table: "product_variants",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "product_variants",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "product_cost_histories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "product_cost_histories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceId",
                table: "product_cost_histories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "product_cost_histories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValuationMethod",
                table: "product_cost_histories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "inventory_valuation_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityAvailable = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AverageCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    InventoryValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValuationMethod = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_valuation_snapshots", x => x.Id);
                    table.CheckConstraint("CK_inventory_valuation_snapshots_available_non_negative", "\"QuantityAvailable\" >= 0");
                    table.CheckConstraint("CK_inventory_valuation_snapshots_on_hand_non_negative", "\"QuantityOnHand\" >= 0");
                    table.CheckConstraint("CK_inventory_valuation_snapshots_reserved_non_negative", "\"QuantityReserved\" >= 0");
                    table.CheckConstraint("CK_inventory_valuation_snapshots_value_non_negative", "\"InventoryValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_inventory_valuation_snapshots_product_variants_ProductVaria~",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_valuation_snapshots_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_prices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceType = table.Column<int>(type: "integer", nullable: false),
                    PriceBeforeVat = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PriceAfterVat = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    ChannelCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_prices", x => x.Id);
                    table.CheckConstraint("CK_product_prices_after_vat_non_negative", "\"PriceAfterVat\" >= 0");
                    table.CheckConstraint("CK_product_prices_before_vat_non_negative", "\"PriceBeforeVat\" >= 0");
                    table.CheckConstraint("CK_product_prices_vat_rate_non_negative", "\"VatRate\" >= 0");
                    table.ForeignKey(
                        name: "FK_product_prices_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_product_variants_current_cost_price_non_negative",
                table: "product_variants",
                sql: "\"CurrentCostPrice\" IS NULL OR \"CurrentCostPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_product_variants_current_selling_price_non_negative",
                table: "product_variants",
                sql: "\"CurrentSellingPrice\" IS NULL OR \"CurrentSellingPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_product_variants_vat_rate_non_negative",
                table: "product_variants",
                sql: "\"VatRate\" IS NULL OR \"VatRate\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_product_cost_histories_ProductVariantId_IsCurrent",
                table: "product_cost_histories",
                columns: new[] { "ProductVariantId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_valuation_snapshots_ProductVariantId_WarehouseId_~",
                table: "inventory_valuation_snapshots",
                columns: new[] { "ProductVariantId", "WarehouseId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_valuation_snapshots_WarehouseId_SnapshotDate",
                table: "inventory_valuation_snapshots",
                columns: new[] { "WarehouseId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_ProductVariantId",
                table: "product_prices",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_ProductVariantId_EffectiveFrom",
                table: "product_prices",
                columns: new[] { "ProductVariantId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_ProductVariantId_IsCurrent",
                table: "product_prices",
                columns: new[] { "ProductVariantId", "IsCurrent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_valuation_snapshots");

            migrationBuilder.DropTable(
                name: "product_prices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_product_variants_current_cost_price_non_negative",
                table: "product_variants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_product_variants_current_selling_price_non_negative",
                table: "product_variants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_product_variants_vat_rate_non_negative",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_cost_histories_ProductVariantId_IsCurrent",
                table: "product_cost_histories");

            migrationBuilder.DropColumn(
                name: "CurrentCostEffectiveFrom",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CurrentCostPrice",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CurrentCostSource",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CurrentPriceEffectiveFrom",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CurrentSellingPrice",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CurrentSellingPriceAfterVat",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CurrentSellingPriceBeforeVat",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "product_cost_histories");

            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "product_cost_histories");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "product_cost_histories");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "product_cost_histories");

            migrationBuilder.DropColumn(
                name: "ValuationMethod",
                table: "product_cost_histories");
        }
    }
}
