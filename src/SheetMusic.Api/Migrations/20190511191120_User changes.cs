using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class Userchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Musicians");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Musicians");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "UserGroups",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "UserGroups",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordHash",
                table: "Musicians",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordSalt",
                table: "Musicians",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Musicians");

            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "Musicians");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Musicians",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Musicians",
                nullable: true);
        }
    }
}
