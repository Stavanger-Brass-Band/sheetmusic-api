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
            var claims = GatherTestUserClaims();
            var testIdentity = new ClaimsIdentity(claims, AuthenticationScheme);
            var testUser = new ClaimsPrincipal(testIdentity);
            var ticket = new AuthenticationTicket(testUser, new AuthenticationProperties(), AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex));
        }
    }

    private List<Claim> GatherTestUserClaims()
    {
        TestUser? testUser;

        try
        {
            var token = Request.Headers["Authorization"].ToString();
            testUser = AuthTokenUtilities.UnwrapAuthToken<TestUser>(token);

            if (testUser is null)
                return new List<Claim>();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, testUser.Identifier.ToString()),
                new Claim(ClaimTypes.Email, testUser.Email ?? "")
            };

            return claims;
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to extract test auth token from [Authorization] header. See inner exception for details.", ex);
        }
    }
}
