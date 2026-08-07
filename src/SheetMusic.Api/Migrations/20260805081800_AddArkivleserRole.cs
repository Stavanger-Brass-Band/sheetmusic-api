using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheetMusic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArkivleserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                // Nobody is auto-promoted - an admin grants Arkivleser manually via PUT users/{id}/roles.
                migrationBuilder.Sql(@"
    IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = 'ARKIVLESER')
        INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
        VALUES ('7a596bb6-bb75-41e1-a299-776990db4d76', 'Arkivleser', 'ARKIVLESER', CONVERT(nvarchar(36), NEWID()));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Roles may predate this migration or receive memberships after it runs, so rollback must not delete them.
        }
    }
}
