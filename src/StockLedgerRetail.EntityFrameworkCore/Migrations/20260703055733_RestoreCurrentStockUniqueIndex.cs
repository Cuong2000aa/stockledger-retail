using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RestoreCurrentStockUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_current_stocks_on_hand_pairs",
                table: "current_stocks");

            migrationBuilder.DropIndex(
                name: "IX_current_stocks_WarehouseId",
                table: "current_stocks");

            migrationBuilder.CreateIndex(
                name: "IX_current_stocks_on_hand_pairs",
                table: "current_stocks",
                columns: new[] { "WarehouseId", "ProductVariantId" },
                filter: "\"QuantityOnHand\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_current_stocks_ProductVariantId_WarehouseId",
                table: "current_stocks",
                columns: new[] { "ProductVariantId", "WarehouseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_current_stocks_on_hand_pairs",
                table: "current_stocks");

            migrationBuilder.DropIndex(
                name: "IX_current_stocks_ProductVariantId_WarehouseId",
                table: "current_stocks");

            migrationBuilder.CreateIndex(
                name: "IX_current_stocks_on_hand_pairs",
                table: "current_stocks",
                columns: new[] { "ProductVariantId", "WarehouseId" },
                unique: true,
                filter: "\"QuantityOnHand\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_current_stocks_WarehouseId",
                table: "current_stocks",
                column: "WarehouseId");
        }
    }
}
