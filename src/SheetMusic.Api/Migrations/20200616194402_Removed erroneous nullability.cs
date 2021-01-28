using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class Removederroneousnullability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SheetMusicCategory");

            migrationBuilder.DropColumn(
                name: "Inactive",
                table: "SheetMusicCategories");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SheetMusicCategories");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "SheetMusicCategories",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SheetMusicId",
                table: "SheetMusicCategories",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SheetMusicSetId",
                table: "SheetMusicCategories",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Inactive = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SheetMusicCategories_CategoryId",
                table: "SheetMusicCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SheetMusicCategories_SheetMusicSetId",
                table: "SheetMusicCategories",
                column: "SheetMusicSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_SheetMusicCategories_Categories_CategoryId",
                table: "SheetMusicCategories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SheetMusicCategories_SheetMusicSets_SheetMusicSetId",
                table: "SheetMusicCategories",
                column: "SheetMusicSetId",
                principalTable: "SheetMusicSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SheetMusicCategories_Categories_CategoryId",
                table: "SheetMusicCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_SheetMusicCategories_SheetMusicSets_SheetMusicSetId",
                table: "SheetMusicCategories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_SheetMusicCategories_CategoryId",
                table: "SheetMusicCategories");

            migrationBuilder.DropIndex(
                name: "IX_SheetMusicCategories_SheetMusicSetId",
                table: "SheetMusicCategories");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "SheetMusicCategories");

            migrationBuilder.DropColumn(
                name: "SheetMusicId",
                table: "SheetMusicCategories");

            migrationBuilder.DropColumn(
                name: "SheetMusicSetId",
                table: "SheetMusicCategories");

            migrationBuilder.AddColumn<bool>(
                name: "Inactive",
                table: "SheetMusicCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SheetMusicCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SheetMusicCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SheetMusicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SheetMusicSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_SheetMusicCategory_CategoryId",
                table: "SheetMusicCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SheetMusicCategory_SheetMusicSetId",
                table: "SheetMusicCategory",
                column: "SheetMusicSetId");
        }
    }
}
