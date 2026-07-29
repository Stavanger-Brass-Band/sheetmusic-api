using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SheetMusic.Api.Users.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Infrastructure.Authentication;

internal class IntgTestAuthenticationHandler(IOptionsMonitor<IntgTestSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<IntgTestSchemeOptions>(options, logger, encoder)
{
    public static string AuthenticationScheme = "IntgTestAuthenticationScheme";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var testUser = ResolveTestUser();

            if (testUser is null)
                return AuthenticateResult.NoResult();

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, testUser.Identifier.ToString()),
                new(ClaimTypes.Email, testUser.Email ?? "")
            };

            // Mirrors the JwtBearer OnTokenValidated enrichment: roles are resolved per request, so a
            // role assigned mid-test takes effect on the next call without re-authenticating.
            var resolver = Context.RequestServices.GetRequiredService<LegacyAuthResolver>();
            var resolved = await resolver.ResolveAsync(testUser.Identifier);

            if (resolved is not null)
                claims.AddRange(resolved.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var testIdentity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(testIdentity);
            var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), AuthenticationScheme);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex);
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
