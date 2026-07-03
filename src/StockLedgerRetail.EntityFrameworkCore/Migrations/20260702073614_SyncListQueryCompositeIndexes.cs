using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SyncListQueryCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_ProductVariantId_WarehouseId_Transaction~",
                table: "stock_transactions",
                columns: new[] { "ProductVariantId", "WarehouseId", "TransactionDate", "CreatedAt" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_WarehouseId_TransactionDate_CreatedAt",
                table: "stock_transactions",
                columns: new[] { "WarehouseId", "TransactionDate", "CreatedAt" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_Status_OrderDate_CreatedAt",
                table: "purchase_orders",
                columns: new[] { "Status", "OrderDate", "CreatedAt" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_WarehouseId_OrderDate_CreatedAt",
                table: "purchase_orders",
                columns: new[] { "WarehouseId", "OrderDate", "CreatedAt" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_Status",
                table: "goods_receipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_Status_ReceiptDate_CreatedAt",
                table: "goods_receipts",
                columns: new[] { "Status", "ReceiptDate", "CreatedAt" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_WarehouseId_ReceiptDate_CreatedAt",
                table: "goods_receipts",
                columns: new[] { "WarehouseId", "ReceiptDate", "CreatedAt" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_ProductVariantId_WarehouseId_Transaction~",
                table: "stock_transactions");

            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_WarehouseId_TransactionDate_CreatedAt",
                table: "stock_transactions");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_Status_OrderDate_CreatedAt",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_WarehouseId_OrderDate_CreatedAt",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipts_Status",
                table: "goods_receipts");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipts_Status_ReceiptDate_CreatedAt",
                table: "goods_receipts");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipts_WarehouseId_ReceiptDate_CreatedAt",
                table: "goods_receipts");
        }
    }
}
