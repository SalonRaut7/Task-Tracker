using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintConstraintsAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sprints_OneActivePerProject",
                table: "Sprints",
                columns: new[] { "ProjectId", "Status" },
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sprints_CreatedAt_UpdatedAt",
                table: "Sprints",
                sql: "\"CreatedAt\" <= \"UpdatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sprints_StartDate_EndDate",
                table: "Sprints",
                sql: "\"StartDate\" <= \"EndDate\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sprints_ValidStatus",
                table: "Sprints",
                sql: "\"Status\" IN (0, 1, 2, 3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sprints_OneActivePerProject",
                table: "Sprints");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sprints_CreatedAt_UpdatedAt",
                table: "Sprints");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sprints_StartDate_EndDate",
                table: "Sprints");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sprints_ValidStatus",
                table: "Sprints");
        }
    }
}
