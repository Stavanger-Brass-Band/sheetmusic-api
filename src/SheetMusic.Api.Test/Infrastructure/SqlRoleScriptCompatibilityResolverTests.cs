extern alias AppHost;

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

            $connectionString = "Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Default;"
            Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlCmd
            """;

        var rewritten = AppHost::SqlRoleScriptCompatibilityResolver.RewriteScriptContent(scriptContent);

        rewritten.Should().NotContain("SqlServer")
            .And.NotContain("Invoke-Sqlcmd")
            .And.Contain("Get-AzAccessToken -ResourceUrl \"https://database.windows.net/\"")
            .And.Contain("System.Data.SqlClient.SqlConnection")
            .And.Contain("Encrypt=True;TrustServerCertificate=False;");
    }
}