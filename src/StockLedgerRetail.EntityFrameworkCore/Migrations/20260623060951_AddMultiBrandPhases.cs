using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiBrandPhases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_variants_Sku",
                table: "product_variants");

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentPriority",
                table: "warehouses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RegionCode",
                table: "warehouses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "product_variants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InTransitWarehouseId",
                table: "inventory_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "inventory_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "inventory_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransferLifecycleStatus",
                table: "inventory_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transfer_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceBrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationBrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowCrossBrand = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transfer_policies_brands_DestinationBrandId",
                        column: x => x.DestinationBrandId,
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfer_policies_brands_SourceBrandId",
                        column: x => x.SourceBrandId,
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_BrandId",
                table: "warehouses",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_Type_BrandId",
                table: "warehouses",
                columns: new[] { "Type", "BrandId" });

            migrationBuilder.CreateIndex(
                name: "IX_products_BrandId",
                table: "products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_BrandId_Sku",
                table: "product_variants",
                columns: new[] { "BrandId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_documents_InTransitWarehouseId",
                table: "inventory_documents",
                column: "InTransitWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_brands_Code",
                table: "brands",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transfer_policies_DestinationBrandId",
                table: "transfer_policies",
                column: "DestinationBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_policies_SourceBrandId_DestinationBrandId_IsActive",
                table: "transfer_policies",
                columns: new[] { "SourceBrandId", "DestinationBrandId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_documents_warehouses_InTransitWarehouseId",
                table: "inventory_documents",
                column: "InTransitWarehouseId",
                principalTable: "warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_products_brands_BrandId",
                table: "products",
                column: "BrandId",
                principalTable: "brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warehouses_brands_BrandId",
                table: "warehouses",
                column: "BrandId",
                principalTable: "brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_documents_warehouses_InTransitWarehouseId",
                table: "inventory_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_products_brands_BrandId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_warehouses_brands_BrandId",
                table: "warehouses");

            migrationBuilder.DropTable(
                name: "transfer_policies");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_BrandId",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_Type_BrandId",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_products_BrandId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_BrandId_Sku",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_inventory_documents_InTransitWarehouseId",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "FulfillmentPriority",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "RegionCode",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "InTransitWarehouseId",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "TransferLifecycleStatus",
                table: "inventory_documents");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Sku",
                table: "product_variants",
                column: "Sku",
                unique: true);
        }
    }
}
