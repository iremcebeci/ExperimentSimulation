using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExperimentSimulation.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class add_content_task_revision_requests_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_task_revision_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContentTaskId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RevisionType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, defaultValue: "Orta")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewDeadline = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Note = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsResolved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_task_revision_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_task_revision_requests_content_tasks_ContentTaskId",
                        column: x => x.ContentTaskId,
                        principalTable: "content_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_task_revision_requests_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_content_task_revision_requests_ContentTaskId_CreatedAtUtc",
                table: "content_task_revision_requests",
                columns: new[] { "ContentTaskId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_content_task_revision_requests_RequestedByUserId",
                table: "content_task_revision_requests",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_task_revision_requests");
        }
    }
}
