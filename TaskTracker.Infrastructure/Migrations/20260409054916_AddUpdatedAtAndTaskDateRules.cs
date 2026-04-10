using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtAndTaskDateRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Tasks\" SET \"UpdatedAt\" = \"CreatedAt\" WHERE \"UpdatedAt\" IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_CreatedAt_UpdatedAt_Consistency",
                table: "Tasks",
                sql: "\"CreatedAt\" <= \"UpdatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_StartDate_EndDate_Consistency",
                table: "Tasks",
                sql: "\"StartDate\" IS NULL OR \"EndDate\" IS NULL OR \"StartDate\" <= \"EndDate\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_CreatedAt_UpdatedAt_Consistency",
                table: "Tasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_StartDate_EndDate_Consistency",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tasks");
        }
    }
}
