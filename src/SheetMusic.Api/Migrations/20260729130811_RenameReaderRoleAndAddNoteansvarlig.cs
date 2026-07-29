using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheetMusic.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameReaderRoleAndAddNoteansvarlig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AspNetUserRoles references roles by ID, so renaming in place keeps every existing membership.
            migrationBuilder.Sql(@"
UPDATE [AspNetRoles]
SET [Name] = 'Musikant', [NormalizedName] = 'MUSIKANT'
WHERE [NormalizedName] = 'READER';");

            // Nobody is auto-promoted - an admin grants Noteansvarlig manually via PUT users/{id}/roles.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = 'NOTEANSVARLIG')
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), 'Noteansvarlig', 'NOTEANSVARLIG', CONVERT(nvarchar(36), NEWID()));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [AspNetUserRoles]
WHERE [RoleId] IN (SELECT [Id] FROM [AspNetRoles] WHERE [NormalizedName] = 'NOTEANSVARLIG');");

            migrationBuilder.Sql(@"
DELETE FROM [AspNetRoles] WHERE [NormalizedName] = 'NOTEANSVARLIG';");

            migrationBuilder.Sql(@"
UPDATE [AspNetRoles]
SET [Name] = 'Reader', [NormalizedName] = 'READER'
WHERE [NormalizedName] = 'MUSIKANT';");
        }
    }
}
