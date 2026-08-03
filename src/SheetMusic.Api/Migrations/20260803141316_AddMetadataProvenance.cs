using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheetMusic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "SheetMusicParts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "SheetMusicParts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "SheetMusicParts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Human");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuggestedAt",
                table: "SheetMusicParts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "SheetMusicCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "SheetMusicCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "SheetMusicCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Human");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuggestedAt",
                table: "SheetMusicCategories",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "SheetMusicParts");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "SheetMusicParts");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SheetMusicParts");

            migrationBuilder.DropColumn(
                name: "SuggestedAt",
                table: "SheetMusicParts");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "SheetMusicCategories");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "SheetMusicCategories");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SheetMusicCategories");

            migrationBuilder.DropColumn(
                name: "SuggestedAt",
                table: "SheetMusicCategories");
        }
    }
}
