using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheetMusic.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyUserHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Musicians_UserGroups_UserGroupId",
                table: "Musicians");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropIndex(
                name: "IX_Musicians_UserGroupId",
                table: "Musicians");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Musicians");

            migrationBuilder.DropColumn(
                name: "Inactive",
                table: "Musicians");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Musicians");

            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "Musicians");

            migrationBuilder.DropColumn(
                name: "UserGroupId",
                table: "Musicians");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Musicians",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Inactive",
                table: "Musicians",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordHash",
                table: "Musicians",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordSalt",
                table: "Musicians",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserGroupId",
                table: "Musicians",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Musicians_UserGroupId",
                table: "Musicians",
                column: "UserGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Musicians_UserGroups_UserGroupId",
                table: "Musicians",
                column: "UserGroupId",
                principalTable: "UserGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
