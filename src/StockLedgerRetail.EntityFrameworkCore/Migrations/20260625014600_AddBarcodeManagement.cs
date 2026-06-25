using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "current_stocks");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "purchase_order_lines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBarcode",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "LotCode",
                table: "inventory_document_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "inventory_document_lines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LotCode",
                table: "goods_receipt_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "goods_receipt_lines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "IsBarcode",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "inventory_document_lines");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "goods_receipt_lines");

            migrationBuilder.AlterColumn<string>(
                name: "LotCode",
                table: "inventory_document_lines",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LotCode",
                table: "goods_receipt_lines",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "current_stocks",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }
    }
}
