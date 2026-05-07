using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskCode",
                table: "Tasks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Backfill existing rows: TaskCode = Project.Key + '-' + Task.Id
            migrationBuilder.Sql(@"
                UPDATE ""Tasks"" t
                SET ""TaskCode"" = p.""Key"" || '-' || t.""Id""::text
                FROM ""Projects"" p
                WHERE t.""ProjectId"" = p.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TaskCode",
                table: "Tasks",
                column: "TaskCode",
                unique: true,
                filter: "\"TaskCode\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_TaskCode",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TaskCode",
                table: "Tasks");
        }
    }
}
