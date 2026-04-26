using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRbacInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvitedByUserId",
                table: "UserProjects",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "UserProjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserProjects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InvitedByUserId",
                table: "UserOrganizations",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "UserOrganizations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserOrganizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    ScopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviteeEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InviteeUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitations_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProjects_InvitedByUserId",
                table: "UserProjects",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizations_InvitedByUserId",
                table: "UserOrganizations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_InvitedByUserId",
                table: "Invitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ScopeType_ScopeId",
                table: "Invitations",
                columns: new[] { "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ScopeType_ScopeId_InviteeEmail",
                table: "Invitations",
                columns: new[] { "ScopeType", "ScopeId", "InviteeEmail" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TokenHash",
                table: "Invitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrganizations_AspNetUsers_InvitedByUserId",
                table: "UserOrganizations",
                column: "InvitedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProjects_AspNetUsers_InvitedByUserId",
                table: "UserProjects",
                column: "InvitedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOrganizations_AspNetUsers_InvitedByUserId",
                table: "UserOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProjects_AspNetUsers_InvitedByUserId",
                table: "UserProjects");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_UserProjects_InvitedByUserId",
                table: "UserProjects");

            migrationBuilder.DropIndex(
                name: "IX_UserOrganizations_InvitedByUserId",
                table: "UserOrganizations");

            migrationBuilder.DropColumn(
                name: "InvitedByUserId",
                table: "UserProjects");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "UserProjects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserProjects");

            migrationBuilder.DropColumn(
                name: "InvitedByUserId",
                table: "UserOrganizations");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "UserOrganizations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserOrganizations");
        }
    }
}
