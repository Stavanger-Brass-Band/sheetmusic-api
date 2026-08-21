using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheetMusic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlwaysDisplayToMusicPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AlwaysDisplay",
                table: "MusicParts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlwaysDisplay",
                table: "MusicParts");
        }
    }
}
