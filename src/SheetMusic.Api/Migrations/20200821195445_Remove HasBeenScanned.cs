using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class RemoveHasBeenScanned : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasBeenScanned",
                table: "SheetMusicSets");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasBeenScanned",
                table: "SheetMusicSets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
