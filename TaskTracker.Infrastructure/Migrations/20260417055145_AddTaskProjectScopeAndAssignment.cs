using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskProjectScopeAndAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Tasks"
                SET "Title" = LEFT("Title", 100)
                WHERE LENGTH("Title") > 100;

                UPDATE "Tasks"
                SET "Description" = LEFT("Description", 500)
                WHERE "Description" IS NOT NULL AND LENGTH("Description") > 500;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tasks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssigneeId",
                table: "Tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EpicId",
                table: "Tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReporterId",
                table: "Tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SprintId",
                table: "Tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    v_task_count integer;
                    v_org_id uuid;
                    v_project_id uuid;
                    v_reporter_id text;
                BEGIN
                    SELECT COUNT(*) INTO v_task_count FROM "Tasks";

                    IF v_task_count > 0 THEN
                        SELECT "Id" INTO v_org_id
                        FROM "Organizations"
                        ORDER BY "CreatedAt", "Id"
                        LIMIT 1;

                        IF v_org_id IS NULL THEN
                            v_org_id := '4d0d7d02-6f85-4f5c-b16f-4e15ddf72352';

                            INSERT INTO "Organizations" ("Id", "Name", "Slug", "Description", "CreatedAt", "UpdatedAt")
                            VALUES (
                                v_org_id,
                                'Legacy Organization',
                                'legacy',
                                'Auto-created during task migration',
                                NOW(),
                                NOW()
                            );
                        END IF;

                        SELECT "Id" INTO v_project_id
                        FROM "Projects"
                        ORDER BY "CreatedAt", "Id"
                        LIMIT 1;

                        IF v_project_id IS NULL THEN
                            v_project_id := '8f4f4d90-878f-4de8-95d1-e450ee23da95';

                            INSERT INTO "Projects" ("Id", "OrganizationId", "Name", "Key", "Description", "CreatedAt", "UpdatedAt")
                            VALUES (
                                v_project_id,
                                v_org_id,
                                'Legacy Project',
                                'LEGACY',
                                'Auto-created during task migration',
                                NOW(),
                                NOW()
                            );
                        END IF;

                        SELECT "Id" INTO v_reporter_id
                        FROM "AspNetUsers"
                        ORDER BY "CreatedAt", "Id"
                        LIMIT 1;

                        IF v_reporter_id IS NULL THEN
                            v_reporter_id := 'legacy-system-user';

                            INSERT INTO "AspNetUsers"
                            (
                                "Id",
                                "UserName",
                                "NormalizedUserName",
                                "Email",
                                "NormalizedEmail",
                                "EmailConfirmed",
                                "PasswordHash",
                                "SecurityStamp",
                                "ConcurrencyStamp",
                                "PhoneNumber",
                                "PhoneNumberConfirmed",
                                "TwoFactorEnabled",
                                "LockoutEnd",
                                "LockoutEnabled",
                                "AccessFailedCount",
                                "FirstName",
                                "LastName",
                                "CreatedAt",
                                "UpdatedAt",
                                "IsActive",
                                "OrganizationId"
                            )
                            VALUES
                            (
                                v_reporter_id,
                                'legacy.system',
                                'LEGACY.SYSTEM',
                                NULL,
                                NULL,
                                FALSE,
                                NULL,
                                'legacy-security-stamp',
                                'legacy-concurrency-stamp',
                                NULL,
                                FALSE,
                                FALSE,
                                NULL,
                                FALSE,
                                0,
                                'Legacy',
                                'System',
                                NOW(),
                                NOW(),
                                TRUE,
                                v_org_id
                            );
                        END IF;

                        UPDATE "Tasks"
                        SET
                            "ProjectId" = COALESCE("ProjectId", v_project_id),
                            "ReporterId" = COALESCE(NULLIF("ReporterId", ''), v_reporter_id)
                        WHERE "ProjectId" IS NULL OR "ReporterId" IS NULL OR "ReporterId" = '';

                        IF EXISTS (
                            SELECT 1
                            FROM "Tasks"
                            WHERE "ProjectId" IS NULL OR "ReporterId" IS NULL OR "ReporterId" = ''
                        ) THEN
                            RAISE EXCEPTION 'Task backfill failed: ProjectId/ReporterId contains null or empty values.';
                        END IF;
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "Tasks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReporterId",
                table: "Tasks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssigneeId",
                table: "Tasks",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_EpicId",
                table: "Tasks",
                column: "EpicId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ReporterId",
                table: "Tasks",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_SprintId",
                table: "Tasks",
                column: "SprintId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_AssigneeId",
                table: "Tasks",
                column: "AssigneeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_ReporterId",
                table: "Tasks",
                column: "ReporterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Epics_EpicId",
                table: "Tasks",
                column: "EpicId",
                principalTable: "Epics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Sprints_SprintId",
                table: "Tasks",
                column: "SprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_AssigneeId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_ReporterId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Epics_EpicId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Sprints_SprintId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_AssigneeId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_EpicId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ReporterId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_SprintId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EpicId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ReporterId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SprintId",
                table: "Tasks");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tasks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
