using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class Initialschema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PartName = table.Column<string>(nullable: true),
                    Aliases = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicParts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SheetMusicCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Inactive = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetMusicCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SheetMusicSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ArchiveNumber = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Composer = table.Column<string>(nullable: true),
                    Arranger = table.Column<string>(nullable: true),
                    SoleSellingAgent = table.Column<string>(nullable: true),
                    MissingParts = table.Column<string>(nullable: true),
                    HasBeenScanned = table.Column<bool>(nullable: false),
                    ArchiveFileName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetMusicSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SheetMusicCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SheetMusicId = table.Column<Guid>(nullable: false),
                    CategoryId = table.Column<Guid>(nullable: false),
                    SheetMusicSetId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetMusicCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SheetMusicCategory_SheetMusicCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SheetMusicCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SheetMusicCategory_SheetMusicSets_SheetMusicSetId",
                        column: x => x.SheetMusicSetId,
                        principalTable: "SheetMusicSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SheetMusicParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    MusicPartId = table.Column<Guid>(nullable: false),
                    SheetMusicId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetMusicParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SheetMusicParts_MusicParts_MusicPartId",
                        column: x => x.MusicPartId,
                        principalTable: "MusicParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SheetMusicParts_SheetMusicSets_SheetMusicId",
                        column: x => x.SheetMusicId,
                        principalTable: "SheetMusicSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Musicians",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserName = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    Inactive = table.Column<bool>(nullable: false),
                    UserGroupId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Musicians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Musicians_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicianMusicPart",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    MusicianId = table.Column<Guid>(nullable: false),
                    MusicPartId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicianMusicPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicianMusicPart_MusicParts_MusicPartId",
                        column: x => x.MusicPartId,
                        principalTable: "MusicParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicianMusicPart_Musicians_MusicianId",
                        column: x => x.MusicianId,
                        principalTable: "Musicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicianMusicPart_MusicPartId",
                table: "MusicianMusicPart",
                column: "MusicPartId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicianMusicPart_MusicianId",
                table: "MusicianMusicPart",
                column: "MusicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Musicians_UserGroupId",
                table: "Musicians",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SheetMusicCategory_CategoryId",
                table: "SheetMusicCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SheetMusicCategory_SheetMusicSetId",
                table: "SheetMusicCategory",
                column: "SheetMusicSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SheetMusicParts_MusicPartId",
                table: "SheetMusicParts",
                column: "MusicPartId");

            migrationBuilder.CreateIndex(
                name: "IX_SheetMusicParts_SheetMusicId",
                table: "SheetMusicParts",
                column: "SheetMusicId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicianMusicPart");

            migrationBuilder.DropTable(
                name: "SheetMusicCategory");

            migrationBuilder.DropTable(
                name: "SheetMusicParts");

            migrationBuilder.DropTable(
                name: "Musicians");

            migrationBuilder.DropTable(
                name: "SheetMusicCategories");

            migrationBuilder.DropTable(
                name: "MusicParts");

            migrationBuilder.DropTable(
                name: "SheetMusicSets");

            migrationBuilder.DropTable(
                name: "UserGroups");
        }
    }
}
