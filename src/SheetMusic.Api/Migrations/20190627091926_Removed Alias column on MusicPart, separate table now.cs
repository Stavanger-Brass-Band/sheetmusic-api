using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class RemovedAliascolumnonMusicPartseparatetablenow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "MusicParts");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aliases",
                table: "MusicParts",
                nullable: true);
        }
    }
}
