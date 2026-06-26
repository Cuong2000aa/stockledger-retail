using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkdownPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "markdown_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WarehouseType = table.Column<int>(type: "integer", nullable: true),
                    LookbackDays = table.Column<int>(type: "integer", nullable: false),
                    MinDaysWithoutOutbound = table.Column<int>(type: "integer", nullable: false),
                    MinOnHand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MinInventoryValueAtCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    MinGrossMarginPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    MaxMarkdownPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    AllowBelowCost = table.Column<bool>(type: "boolean", nullable: false),
                    RequireApprovalAbovePercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    SlowSellThroughThreshold = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    TiersJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_markdown_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_markdown_policies_brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_markdown_policies_BrandId_RegionCode_WarehouseType_IsActive",
                table: "markdown_policies",
                columns: new[] { "BrandId", "RegionCode", "WarehouseType", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "markdown_policies");
        }
    }
}
