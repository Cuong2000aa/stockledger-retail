using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddLotExpiryAndApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "purchase_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedApprovalSteps",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstApprovedAt",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstApprovedBy",
                table: "purchase_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalSteps",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TrackLotExpiry",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CompletedApprovalSteps",
                table: "inventory_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstApprovedAt",
                table: "inventory_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstApprovedBy",
                table: "inventory_documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalSteps",
                table: "inventory_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "inventory_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedBy",
                table: "inventory_documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "inventory_document_lines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotCode",
                table: "inventory_document_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockLotId",
                table: "inventory_document_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "goods_receipt_lines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotCode",
                table: "goods_receipt_lines",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stock_lots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ManufacturedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_lots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_lots_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lot_stocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lot_stocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lot_stocks_stock_lots_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "stock_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lot_stocks_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_document_lines_StockLotId",
                table: "inventory_document_lines",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_lot_stocks_StockLotId_WarehouseId",
                table: "lot_stocks",
                columns: new[] { "StockLotId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lot_stocks_WarehouseId",
                table: "lot_stocks",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_lots_ProductVariantId_LotCode",
                table: "stock_lots",
                columns: new[] { "ProductVariantId", "LotCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_document_lines_stock_lots_StockLotId",
                table: "inventory_document_lines",
                column: "StockLotId",
                principalTable: "stock_lots",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_document_lines_stock_lots_StockLotId",
                table: "inventory_document_lines");

            migrationBuilder.DropTable(
                name: "lot_stocks");

            migrationBuilder.DropTable(
                name: "stock_lots");

            migrationBuilder.DropIndex(
                name: "IX_inventory_document_lines_StockLotId",
                table: "inventory_document_lines");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "CompletedApprovalSteps",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "FirstApprovedAt",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "FirstApprovedBy",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalSteps",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "TrackLotExpiry",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CompletedApprovalSteps",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "FirstApprovedAt",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "FirstApprovedBy",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalSteps",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "inventory_document_lines");

            migrationBuilder.DropColumn(
                name: "LotCode",
                table: "inventory_document_lines");

            migrationBuilder.DropColumn(
                name: "StockLotId",
                table: "inventory_document_lines");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "LotCode",
                table: "goods_receipt_lines");
        }
    }
}
