using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUnitBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "inventory_document_lines");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "goods_receipt_lines");

            migrationBuilder.CreateTable(
                name: "goods_receipt_line_barcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_line_barcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goods_receipt_line_barcodes_goods_receipt_lines_GoodsReceip~",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "goods_receipt_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_document_line_barcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryDocumentLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_document_line_barcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_document_line_barcodes_inventory_document_lines_I~",
                        column: x => x.InventoryDocumentLineId,
                        principalTable: "inventory_document_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_line_barcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_line_barcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_order_line_barcodes_purchase_order_lines_PurchaseO~",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variant_unit_barcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variant_unit_barcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_variant_unit_barcodes_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_variant_unit_barcodes_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_barcodes_Barcode",
                table: "goods_receipt_line_barcodes",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_barcodes_GoodsReceiptLineId",
                table: "goods_receipt_line_barcodes",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_document_line_barcodes_Barcode",
                table: "inventory_document_line_barcodes",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_document_line_barcodes_InventoryDocumentLineId",
                table: "inventory_document_line_barcodes",
                column: "InventoryDocumentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_line_barcodes_Barcode",
                table: "purchase_order_line_barcodes",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_line_barcodes_PurchaseOrderLineId",
                table: "purchase_order_line_barcodes",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_variant_unit_barcodes_Barcode",
                table: "variant_unit_barcodes",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variant_unit_barcodes_ProductVariantId",
                table: "variant_unit_barcodes",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_variant_unit_barcodes_ProductVariantId_WarehouseId_Status",
                table: "variant_unit_barcodes",
                columns: new[] { "ProductVariantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_variant_unit_barcodes_WarehouseId",
                table: "variant_unit_barcodes",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "goods_receipt_line_barcodes");

            migrationBuilder.DropTable(
                name: "inventory_document_line_barcodes");

            migrationBuilder.DropTable(
                name: "purchase_order_line_barcodes");

            migrationBuilder.DropTable(
                name: "variant_unit_barcodes");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "purchase_order_lines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "inventory_document_lines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "goods_receipt_lines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
