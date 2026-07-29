-- Anonymises a copy of the production database for use as test data (issue #246, moved here from #244
-- per #234 decision 14). Run this against a *restored copy* of production, never against production
-- itself, before loading the result into the test environment. The sheet music catalogue (Projects,
-- ProjectSheetMusicSets, SheetMusicSets, SheetMusicParts, SheetMusicCategories, MusicParts,
-- MusicPartAliases, MusicianMusicParts, Categories, UserGroups) is left untouched - only person-
-- identifying and credential data is rewritten.
--
-- Legacy v1 (Musician-based) login is out of scope: the new Aspire-hosted environment (issue #246)
-- does not need to support it, so this script does not anonymise dbo.Musicians' obsolete
-- Email/PasswordHash/PasswordSalt columns - only dbo.AspNetUsers (ASP.NET Core Identity /
-- ApplicationUser, the only login path this environment supports) matters here.
--
-- This doubles as a dry run of the #247 export/import path.
--
-- Requires two sqlcmd variables (this script must be run in SQLCMD mode, e.g. via `sqlcmd -v` or SSMS
-- with SQLCMD Mode enabled) - neither is committed here:
--   AdminEmail          - the email/username the seeded test administrator signs in with.
--   AdminPasswordHash   - an ASP.NET Core Identity v3 password hash (NOT the plaintext password) for a
--                         non-production password. Generate one locally, once, with a throwaway console
--                         app referencing Microsoft.Extensions.Identity.Core:
--
--                             var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();
--                             Console.WriteLine(hasher.HashPassword(null!, "SomeNonProductionPassword123!"));
--
--                         Never commit the plaintext password or the hash to source control - pass
--                         AdminPasswordHash in on the command line each time this script is run.
--
-- Example invocation:
--   sqlcmd -S <server> -d <database> -i anonymize-test-data.sql ^
--     -v AdminEmail="admin@invalid.example" AdminPasswordHash="<hash from the snippet above>"

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- ASP.NET Core Identity users (SheetMusic.Api.Database.Entities.ApplicationUser). Emails move to a
-- non-routable domain per RFC 2606 so anonymised test data can never accidentally send a real email
-- (see Email__* AppHost parameters and IEmailSender) or collide with a real address in
-- RequireUniqueEmail validation. PasswordHash is blanked outright, then one account is re-enabled below
-- so test remains reachable.
UPDATE dbo.AspNetUsers
SET
    Email = CONCAT('user-', Id, '@invalid.example'),
    NormalizedEmail = CONCAT('USER-', UPPER(CONVERT(nvarchar(36), Id)), '@INVALID.EXAMPLE'),
    UserName = CASE WHEN UserName LIKE '%@%'
        THEN CONCAT('user-', Id, '@invalid.example')
        ELSE UserName
    END,
    NormalizedUserName = CASE WHEN NormalizedUserName LIKE '%@%'
        THEN CONCAT('USER-', UPPER(CONVERT(nvarchar(36), Id)), '@INVALID.EXAMPLE')
        ELSE NormalizedUserName
    END,
    PasswordHash = NULL,
    PhoneNumber = NULL,
    -- Invalidates any existing "remember me"/two-factor state tied to the real credential.
    SecurityStamp = CONVERT(nvarchar(36), NEWID()),
    ConcurrencyStamp = CONVERT(nvarchar(36), NEWID());

-- Refresh tokens are bearer credentials for the *real* environment; they must not survive the copy.
DELETE FROM dbo.RefreshTokens;

-- Sign-in after blanking password hashes (issue #246 decision, settled): seed one known administrator
-- rather than requiring Google-only auth (this app authenticates via ASP.NET Core Identity's password
-- flow - see the Users.Commands.Login handler - not Google). Promotes whichever account already
-- holds the "Admin" role (see Roles.Admin/AddSheetMusicSecurity) in the copied data, so its
-- existing AspNetUserRoles membership carries over unchanged - only that one account's email and
-- password become known/usable; no new row is inserted.
DECLARE @AdminEmail nvarchar(256) = N'$(AdminEmail)';
DECLARE @AdminPasswordHash nvarchar(max) = N'$(AdminPasswordHash)';
DECLARE @AdminUserId uniqueidentifier;

IF @AdminEmail IS NULL OR @AdminEmail = N'' OR @AdminEmail = N'$' + N'(AdminEmail)'
    OR @AdminPasswordHash IS NULL OR @AdminPasswordHash = N'' OR @AdminPasswordHash = N'$' + N'(AdminPasswordHash)'
BEGIN
    RAISERROR('AdminEmail and AdminPasswordHash sqlcmd variables must be supplied - see the header comment for how to generate a password hash.', 16, 1);
END

SELECT TOP 1 @AdminUserId = ur.UserId
FROM dbo.AspNetUserRoles ur
JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
WHERE r.NormalizedName = N'ADMIN'
ORDER BY ur.UserId;

IF @AdminUserId IS NULL
BEGIN
    RAISERROR('No existing Admin-role user found in this copy to promote - seed one in the source environment first.', 16, 1);
END

UPDATE dbo.AspNetUsers
SET
    Email = @AdminEmail,
    NormalizedEmail = UPPER(@AdminEmail),
    UserName = @AdminEmail,
    NormalizedUserName = UPPER(@AdminEmail),
    EmailConfirmed = 1,
    PasswordHash = @AdminPasswordHash,
    LockoutEnd = NULL,
    AccessFailedCount = 0
WHERE Id = @AdminUserId;

COMMIT TRANSACTION;
