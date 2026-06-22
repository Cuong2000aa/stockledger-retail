using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<int>(type: "integer", nullable: false),
                    ReferenceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CommittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_reservations_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservation_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservation_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_reservation_lines_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservation_lines_stock_reservations_StockReservation~",
                        column: x => x.StockReservationId,
                        principalTable: "stock_reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservation_lines_ProductVariantId",
                table: "stock_reservation_lines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservation_lines_StockReservationId_ProductVariantId",
                table: "stock_reservation_lines",
                columns: new[] { "StockReservationId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ReservationNo",
                table: "stock_reservations",
                column: "ReservationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_SourceSystem_ReferenceType_ReferenceKey_~",
                table: "stock_reservations",
                columns: new[] { "SourceSystem", "ReferenceType", "ReferenceKey", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_WarehouseId",
                table: "stock_reservations",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_reservation_lines");

            migrationBuilder.DropTable(
                name: "stock_reservations");
        }
    }
}
