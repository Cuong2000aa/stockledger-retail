using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInsightActionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "insight_action_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InsightKind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionStatus = table.Column<int>(type: "integer", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    ResultEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultEntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insight_action_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_insight_action_logs_ActionStatus",
                table: "insight_action_logs",
                column: "ActionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_insight_action_logs_CreatedAt",
                table: "insight_action_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_insight_action_logs_InsightKind",
                table: "insight_action_logs",
                column: "InsightKind");

            migrationBuilder.CreateIndex(
                name: "IX_insight_action_logs_ProductVariantId_WarehouseId",
                table: "insight_action_logs",
                columns: new[] { "ProductVariantId", "WarehouseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "insight_action_logs");
        }
    }
}
