using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Infrastructure.Authentication;

internal class IntgTestAuthenticationHandler(IOptionsMonitor<IntgTestSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<IntgTestSchemeOptions>(options, logger, encoder)
{
    public static string AuthenticationScheme = "IntgTestAuthenticationScheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var testUser = ResolveTestUser();

            if (testUser is null)
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, testUser.Identifier.ToString()),
                new(ClaimTypes.Email, testUser.Email ?? "")
            };

            var testIdentity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(testIdentity);
            var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex));
        }
    }

    private TestUser? ResolveTestUser()
    {
        try
        {
            var token = Request.Headers.Authorization.ToString();
            return AuthTokenUtilities.UnwrapAuthToken<TestUser>(token);
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to extract test auth token from [Authorization] header. See inner exception for details.", ex);
        }
    }
}
