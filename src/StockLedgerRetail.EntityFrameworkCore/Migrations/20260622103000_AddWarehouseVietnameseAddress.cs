using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public class AddWarehouseVietnameseAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "warehouses",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "warehouses",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "warehouses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullAddress",
                table: "warehouses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "warehouses",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "warehouses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "warehouses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "warehouses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "District",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "FullAddress",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "warehouses");
        }
    }
}
