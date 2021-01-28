using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class AddedprojectEntityandconnectiontoSet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    StartDate = table.Column<DateTime>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSheetMusicSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SheetMusicSetId = table.Column<Guid>(nullable: false),
                    ProjecId = table.Column<Guid>(nullable: false),
                    ProjectId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSheetMusicSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectSheetMusicSets_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectSheetMusicSets_SheetMusicSets_SheetMusicSetId",
                        column: x => x.SheetMusicSetId,
                        principalTable: "SheetMusicSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSheetMusicSets_ProjectId",
                table: "ProjectSheetMusicSets",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSheetMusicSets_SheetMusicSetId",
                table: "ProjectSheetMusicSets",
                column: "SheetMusicSetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectSheetMusicSets");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
