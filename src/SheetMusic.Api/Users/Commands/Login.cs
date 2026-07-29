using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Users.Errors;
using SheetMusic.Api.Users.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Commands;

/// <summary>
/// Authenticates a user by username/password (the OAuth2 "basic" grant accepted by
/// <c>UsersController.AuthenticateAsync</c>) and issues a fresh access token/refresh token pair.
/// </summary>
public class Login(string username, string password) : IRequest<ApiAccessTokens>
{
    public string Username { get; } = username;
    public string Password { get; } = password;

    public class Handler(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, SheetMusicContext db, IConfiguration configuration) : IRequestHandler<Login, ApiAccessTokens>
    {
        public async Task<ApiAccessTokens> Handle(Login request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Username);

            if (user == null || user.Inactive)
                throw new InvalidCredentialsError();

            // lockoutOnFailure: true increments the failed access count and locks the account after
            // IdentityOptions.Lockout.MaxFailedAccessAttempts is reached. The response is intentionally
            // identical to the generic invalid-credentials case to avoid leaking account lockout state
            // to an unauthenticated caller.
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
                throw new InvalidCredentialsError();

            var (refreshToken, rawRefreshToken) = AccessTokenFactory.CreateRefreshToken(user.Id, configuration);
            db.RefreshTokens.Add(refreshToken);
            await db.SaveChangesAsync(cancellationToken);

            var (accessToken, expiresAt) = AccessTokenFactory.CreateAccessToken(user.Id, configuration);

            return new ApiAccessTokens
            {
                access_token = accessToken,
                refresh_token = rawRefreshToken,
                token_type = "bearer",
                expires_in = (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
                scope = "sheetmusic-api"
            };
        }
    }
}
