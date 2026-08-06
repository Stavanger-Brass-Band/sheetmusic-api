extern alias AppHost;

using System;
using FluentAssertions;
using Xunit;

namespace SheetMusic.Api.Test.Infrastructure;

public class SqlRoleScriptCompatibilityResolverTests
{
    [Fact]
    public void RewriteScriptContent_UsesInBoxSqlClient_WhenGeneratedScriptImportsSqlServerModule()
    {
        const string scriptContent = """
            Install-Module -Name SqlServer -RequiredVersion 22.3.0 -Force -AllowClobber -Scope CurrentUser
            Import-Module SqlServer

            $sqlCmd = @"
            DECLARE @name SYSNAME = '$principalName';
            DECLARE @id UNIQUEIDENTIFIER = '$id';
            DECLARE @castId NVARCHAR(MAX) = CONVERT(VARCHAR(MAX), CONVERT (VARBINARY(16), @id), 1);
            DECLARE @cmd NVARCHAR(MAX) = N'CREATE USER [' + @name + '] WITH SID = ' + @castId + ', TYPE = E;'
            EXEC (@cmd);
            DECLARE @role1 NVARCHAR(MAX) = N'ALTER ROLE db_owner ADD MEMBER [' + @name + ']';
            EXEC (@role1);
            "@

            $connectionString = "Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Default;"
            Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlCmd
            """;

        var rewritten = AppHost::SqlRoleScriptCompatibilityResolver.RewriteScriptContent(scriptContent);

        rewritten.Should().NotContain("SqlServer")
            .And.NotContain("Invoke-Sqlcmd")
            .And.Contain("Get-AzAccessToken -ResourceUrl \"https://database.windows.net/\"")
            .And.Contain("System.Data.SqlClient.SqlConnection")
            .And.Contain("Encrypt=True;TrustServerCertificate=False;")
            .And.Contain("IF @existingSid IS NULL")
            .And.Contain("sys.database_role_members")
            .And.Contain("@existingPrincipalType")
            .And.Contain("THROW 50000")
            .And.NotContain("DECLARE @cmd NVARCHAR(MAX)");
    }

    [Fact]
    public void RewriteScriptContent_Throws_WhenGeneratedCreateUserBatchIsUnrecognized()
    {
        const string scriptContent = """
            $sqlCmd = @"
            CREATE -- unsupported user
            USER [unexpected] WITH SID = 0x00, TYPE = E;
            "@
            Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlCmd
            """;

        var rewrite = () => AppHost::SqlRoleScriptCompatibilityResolver.RewriteScriptContent(scriptContent);

        rewrite.Should().Throw<InvalidOperationException>()
            .WithMessage("*unsupported CREATE USER batch*");
    }

    [Fact]
    public void RewriteScriptContent_RewritesLegacyBatch_WhenGeneratedSqlUsesLowercaseKeywords()
    {
        const string scriptContent = """
            $sqlCmd = @"
            declare @name sysname = '$principalName';
            declare @id uniqueidentifier = '$id';
            declare @castId nvarchar(max) = convert(varchar(max), convert (varbinary(16), @id), 1);
            declare @cmd nvarchar(max) = N'create user [' + @name + '] with sid = ' + @castId + ', type = E;'
            exec (@cmd);
            declare @role1 nvarchar(max) = N'alter role db_owner add member [' + @name + ']';
            exec (@role1);
            "@
            Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlCmd
            """;

        var rewritten = AppHost::SqlRoleScriptCompatibilityResolver.RewriteScriptContent(scriptContent);

        rewritten.Should().Contain("IF @existingSid IS NULL")
            .And.NotContain("declare @cmd nvarchar(max)");
    }

    [Fact]
    public void RewriteScriptContent_Throws_WhenRecognizedAndUnrecognizedCreateUserBatchesAreMixed()
    {
        const string scriptContent = """
            $sqlCmd = @"
            DECLARE @name SYSNAME = '$principalName';
            DECLARE @id UNIQUEIDENTIFIER = '$id';
            DECLARE @castId NVARCHAR(MAX) = CONVERT(VARCHAR(MAX), CONVERT (VARBINARY(16), @id), 1);
            DECLARE @cmd NVARCHAR(MAX) = N'CREATE USER [' + @name + '] WITH SID = ' + @castId + ', TYPE = E;'
            EXEC (@cmd);
            DECLARE @role1 NVARCHAR(MAX) = N'ALTER ROLE db_owner ADD MEMBER [' + @name + ']';
            EXEC (@role1);
            CREATE USER [unexpected] WITH SID = 0x00, TYPE = E;
            "@
            Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlCmd
            """;

        var rewrite = () => AppHost::SqlRoleScriptCompatibilityResolver.RewriteScriptContent(scriptContent);

        rewrite.Should().Throw<InvalidOperationException>()
            .WithMessage("*unsupported CREATE USER batch*");
    }
}