using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class FixedprojectIdtypeO : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectSheetMusicSets_Projects_ProjectId",
                table: "ProjectSheetMusicSets");

            migrationBuilder.DropColumn(
                name: "ProjecId",
                table: "ProjectSheetMusicSets");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ProjectSheetMusicSets",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectSheetMusicSets_Projects_ProjectId",
                table: "ProjectSheetMusicSets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectSheetMusicSets_Projects_ProjectId",
                table: "ProjectSheetMusicSets");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ProjectSheetMusicSets",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjecId",
                table: "ProjectSheetMusicSets",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectSheetMusicSets_Projects_ProjectId",
                table: "ProjectSheetMusicSets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
