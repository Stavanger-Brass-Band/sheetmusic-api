using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class Renamedcolumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SheetMusicParts_SheetMusicSets_SheetMusicId",
                table: "SheetMusicParts");

            migrationBuilder.RenameColumn(
                name: "SheetMusicId",
                table: "SheetMusicParts",
                newName: "SetId");

            migrationBuilder.RenameIndex(
                name: "IX_SheetMusicParts_SheetMusicId",
                table: "SheetMusicParts",
                newName: "IX_SheetMusicParts_SetId");

            migrationBuilder.AddForeignKey(
                name: "FK_SheetMusicParts_SheetMusicSets_SetId",
                table: "SheetMusicParts",
                column: "SetId",
                principalTable: "SheetMusicSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SheetMusicParts_SheetMusicSets_SetId",
                table: "SheetMusicParts");

            migrationBuilder.RenameColumn(
                name: "SetId",
                table: "SheetMusicParts",
                newName: "SheetMusicId");

            migrationBuilder.RenameIndex(
                name: "IX_SheetMusicParts_SetId",
                table: "SheetMusicParts",
                newName: "IX_SheetMusicParts_SheetMusicId");

            migrationBuilder.AddForeignKey(
                name: "FK_SheetMusicParts_SheetMusicSets_SheetMusicId",
                table: "SheetMusicParts",
                column: "SheetMusicId",
                principalTable: "SheetMusicSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
