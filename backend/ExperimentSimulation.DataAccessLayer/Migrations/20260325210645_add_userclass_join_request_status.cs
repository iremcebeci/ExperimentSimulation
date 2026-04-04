using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExperimentSimulation.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class add_userclass_join_request_status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MemberRole",
                table: "userclass",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "userclass",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "userclass",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.Sql("UPDATE `userclass` SET `RequestedAt` = COALESCE(`JoinedAt`, UTC_TIMESTAMP()) WHERE `RequestedAt` IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RequestedAt",
                table: "userclass",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "userclass",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Approved")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "userclass");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "userclass");

            migrationBuilder.AlterColumn<string>(
                name: "MemberRole",
                table: "userclass",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "userclass",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);
        }
    }
}
