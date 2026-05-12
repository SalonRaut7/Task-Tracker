using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintArchiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "Sprints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUTC",
                table: "Sprints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "Sprints",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUTC",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "Sprints");
        }
    }
}
