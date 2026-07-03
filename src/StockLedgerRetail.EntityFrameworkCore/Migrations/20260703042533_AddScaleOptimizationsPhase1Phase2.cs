using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddScaleOptimizationsPhase1Phase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_current_stocks_ProductVariantId_WarehouseId",
                table: "current_stocks");

            migrationBuilder.CreateTable(
                name: "inventory_daily_rollups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SkuCount = table.Column<int>(type: "integer", nullable: false),
                    TotalOnHand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAvailable = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalInventoryValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OutboundQty30d = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_daily_rollups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_outbound_pairs",
                table: "stock_transactions",
                columns: new[] { "ProductVariantId", "WarehouseId", "TransactionDate" },
                descending: new[] { false, false, true },
                filter: "\"TransactionType\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_current_stocks_on_hand_pairs",
                table: "current_stocks",
                columns: new[] { "ProductVariantId", "WarehouseId" },
                unique: true,
                filter: "\"QuantityOnHand\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_daily_rollups_BrandId_SnapshotDate",
                table: "inventory_daily_rollups",
                columns: new[] { "BrandId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_daily_rollups_SnapshotDate_BrandId_WarehouseId_Re~",
                table: "inventory_daily_rollups",
                columns: new[] { "SnapshotDate", "BrandId", "WarehouseId", "RegionCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_daily_rollups");

            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_outbound_pairs",
                table: "stock_transactions");

            migrationBuilder.DropIndex(
                name: "IX_current_stocks_on_hand_pairs",
                table: "current_stocks");

            migrationBuilder.CreateIndex(
                name: "IX_current_stocks_ProductVariantId_WarehouseId",
                table: "current_stocks",
                columns: new[] { "ProductVariantId", "WarehouseId" },
                unique: true);
        }
    }
}
