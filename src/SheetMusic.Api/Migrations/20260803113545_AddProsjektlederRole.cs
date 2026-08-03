using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheetMusic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProsjektlederRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nobody is auto-promoted - an admin grants Prosjektleder manually via PUT users/{id}/roles.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = 'PROSJEKTLEDER')
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), 'Prosjektleder', 'PROSJEKTLEDER', CONVERT(nvarchar(36), NEWID()));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [AspNetUserRoles]
WHERE [RoleId] IN (SELECT [Id] FROM [AspNetRoles] WHERE [NormalizedName] = 'PROSJEKTLEDER');");

            migrationBuilder.Sql(@"
DELETE FROM [AspNetRoles] WHERE [NormalizedName] = 'PROSJEKTLEDER';");
        }
    }
}
