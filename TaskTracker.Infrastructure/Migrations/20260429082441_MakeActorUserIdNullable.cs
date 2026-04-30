using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeActorUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_ActorUserId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "ActorUserId",
                table: "Notifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_ActorUserId",
                table: "Notifications",
                column: "ActorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_ActorUserId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "ActorUserId",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_ActorUserId",
                table: "Notifications",
                column: "ActorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
