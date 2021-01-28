using Microsoft.EntityFrameworkCore.Migrations;

namespace SheetMusic.Api.Migrations
{
    public partial class Usergroupchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "UserGroups");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "UserGroups",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "UserGroups",
                nullable: true);
        }
    }
}
