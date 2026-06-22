using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryValuation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "product_variants",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CostSource",
                table: "product_variants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellingPrice",
                table: "product_variants",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_cost_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CostSource = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_cost_histories", x => x.Id);
                    table.CheckConstraint("CK_product_cost_history_cost_price_non_negative", "\"CostPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_product_cost_histories_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_product_variants_cost_price_non_negative",
                table: "product_variants",
                sql: "\"CostPrice\" IS NULL OR \"CostPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_product_variants_selling_price_non_negative",
                table: "product_variants",
                sql: "\"SellingPrice\" IS NULL OR \"SellingPrice\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_product_cost_histories_ProductVariantId",
                table: "product_cost_histories",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_cost_histories_ProductVariantId_EffectiveFrom",
                table: "product_cost_histories",
                columns: new[] { "ProductVariantId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_cost_histories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_product_variants_cost_price_non_negative",
                table: "product_variants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_product_variants_selling_price_non_negative",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CostSource",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "SellingPrice",
                table: "product_variants");
        }
    }
}
