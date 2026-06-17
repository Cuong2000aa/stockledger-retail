using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryDocumentSourceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "inventory_documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_documents_SourceSystem_ReferenceNo_DocumentType",
                table: "inventory_documents",
                columns: new[] { "SourceSystem", "ReferenceNo", "DocumentType" },
                unique: true,
                filter: "\"ReferenceNo\" IS NOT NULL AND \"SourceSystem\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_documents_SourceSystem_ReferenceNo_DocumentType",
                table: "inventory_documents");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "inventory_documents");
        }
    }
}
