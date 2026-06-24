using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "background_job_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TriggeredBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_background_job_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "background_job_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LastMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastRunStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_background_job_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_background_job_runs_JobKey_StartedAtUtc",
                table: "background_job_runs",
                columns: new[] { "JobKey", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_background_job_settings_JobKey",
                table: "background_job_settings",
                column: "JobKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "background_job_runs");

            migrationBuilder.DropTable(
                name: "background_job_settings");
        }
    }
}
