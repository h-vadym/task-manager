using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Task.Analytics.Storage.Migrations
{
    /// <inheritdoc />
    public partial class TaskTypeStatsAggregation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_analytics");

            migrationBuilder.DropColumn(
                name: "aggregate_id",
                table: "processed_messages");

            migrationBuilder.DropColumn(
                name: "event_type",
                table: "processed_messages");

            migrationBuilder.DropColumn(
                name: "occurred_at",
                table: "processed_messages");

            migrationBuilder.CreateTable(
                name: "task_type_stats",
                columns: table => new
                {
                    task_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    tasks_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_type_stats", x => new { x.task_type, x.status });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_type_stats");

            migrationBuilder.AddColumn<string>(
                name: "aggregate_id",
                table: "processed_messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                table: "processed_messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "occurred_at",
                table: "processed_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "task_analytics",
                columns: table => new
                {
                    task_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    task_status = table.Column<string>(type: "text", nullable: false),
                    task_type = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_analytics", x => x.task_id);
                });
        }
    }
}
