using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "app_users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "app_users");
        }
    }
}
