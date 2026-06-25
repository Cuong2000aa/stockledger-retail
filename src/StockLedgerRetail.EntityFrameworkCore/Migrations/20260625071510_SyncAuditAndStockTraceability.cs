using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SyncAuditAndStockTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "warehouses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "warehouses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "teams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "teams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CounterpartWarehouseId",
                table: "stock_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNo",
                table: "stock_transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNo",
                table: "stock_transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "stock_transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "purchase_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "product_variants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "product_variants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "permission_groups",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "permission_groups",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "inventory_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "inventory_documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "goods_receipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "goods_receipts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "brands",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "brands",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "app_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "app_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stock_transaction_barcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transaction_barcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_transaction_barcodes_stock_transactions_StockTransact~",
                        column: x => x.StockTransactionId,
                        principalTable: "stock_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_CounterpartWarehouseId",
                table: "stock_transactions",
                column: "CounterpartWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_DocumentNo",
                table: "stock_transactions",
                column: "DocumentNo");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transaction_barcodes_Barcode",
                table: "stock_transaction_barcodes",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transaction_barcodes_StockTransactionId",
                table: "stock_transaction_barcodes",
                column: "StockTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_transactions_warehouses_CounterpartWarehouseId",
                table: "stock_transactions",
                column: "CounterpartWarehouseId",
                principalTable: "warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_transactions_warehouses_CounterpartWarehouseId",
                table: "stock_transactions");

            migrationBuilder.DropTable(
                name: "stock_transaction_barcodes");

            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_CounterpartWarehouseId",
                table: "stock_transactions");

            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_DocumentNo",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "CounterpartWarehouseId",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "DocumentNo",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "ReferenceNo",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "permission_groups");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "permission_groups");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "app_users");
        }
    }
}
