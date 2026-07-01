using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class AddTransactionLogCompositeIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_transaction_logs_EntityName_CreatedAt",
            table: "transaction_logs",
            columns: new[] { "EntityName", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_transaction_logs_CreatedBy_CreatedAt",
            table: "transaction_logs",
            columns: new[] { "CreatedBy", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_transaction_logs_Action_CreatedAt",
            table: "transaction_logs",
            columns: new[] { "Action", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_transaction_logs_EntityName_CreatedAt",
            table: "transaction_logs");

        migrationBuilder.DropIndex(
            name: "IX_transaction_logs_CreatedBy_CreatedAt",
            table: "transaction_logs");

        migrationBuilder.DropIndex(
            name: "IX_transaction_logs_Action_CreatedAt",
            table: "transaction_logs");
    }
}
