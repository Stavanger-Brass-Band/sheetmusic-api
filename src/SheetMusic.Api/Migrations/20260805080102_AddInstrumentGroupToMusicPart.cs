using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheetMusic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentGroupToMusicPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstrumentGroup",
                table: "MusicParts",
                type: "varchar(50)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstrumentGroup",
                table: "MusicParts");
        }
    }
}
