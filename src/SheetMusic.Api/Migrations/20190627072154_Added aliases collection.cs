using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class Addedaliasescollection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicPartAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Alias = table.Column<string>(nullable: true),
                    Enabled = table.Column<bool>(nullable: false),
                    MusicPartId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPartAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicPartAliases_MusicParts_MusicPartId",
                        column: x => x.MusicPartId,
                        principalTable: "MusicParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicPartAliases_MusicPartId",
                table: "MusicPartAliases",
                column: "MusicPartId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicPartAliases");
        }
    }
}
