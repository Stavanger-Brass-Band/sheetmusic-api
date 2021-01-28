using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class Addedborrowdcolumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchiveFileName",
                table: "SheetMusicSets");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "SheetMusicSets",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BorrowedDateTime",
                table: "SheetMusicSets",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BorrowedFrom",
                table: "SheetMusicSets",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BorrowedDateTime",
                table: "SheetMusicSets");

            migrationBuilder.DropColumn(
                name: "BorrowedFrom",
                table: "SheetMusicSets");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "SheetMusicSets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<string>(
                name: "ArchiveFileName",
                table: "SheetMusicSets",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
