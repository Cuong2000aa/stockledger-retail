using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInsightSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "insight_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InsightKind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insight_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_insight_snapshots_InsightKind_GeneratedAtUtc",
                table: "insight_snapshots",
                columns: new[] { "InsightKind", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_insight_snapshots_SnapshotKey",
                table: "insight_snapshots",
                column: "SnapshotKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "insight_snapshots");
        }
    }
}
