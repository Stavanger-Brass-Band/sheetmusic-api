using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
/// Exchanges a valid, active refresh token for a new access token. Rotates the refresh token in the
/// same operation: the presented token is revoked and a brand new one is issued and returned, so a
/// stolen-but-unused refresh token can only ever be redeemed once (see notes on issue #119 - reusing a
/// revoked token is rejected by <see cref="Database.Entities.RefreshToken.IsActive"/>).
/// </summary>
public class RefreshAccessToken(string refreshToken) : IRequest<ApiAccessTokens>
{
    public string RefreshToken { get; } = refreshToken;

    public class Handler(SheetMusicContext db, UserManager<ApplicationUser> userManager, IConfiguration configuration) : IRequestHandler<RefreshAccessToken, ApiAccessTokens>
    {
        public async Task<ApiAccessTokens> Handle(RefreshAccessToken request, CancellationToken cancellationToken)
        {
            var hashedToken = AccessTokenFactory.HashToken(request.RefreshToken);

            var existingToken = await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);

            if (existingToken == null || !existingToken.IsActive)
                throw new InvalidRefreshTokenError();

            var user = await userManager.FindByIdAsync(existingToken.UserId.ToString());

            if (user == null || user.Inactive)
                throw new InvalidRefreshTokenError();

            existingToken.RevokedAt = DateTime.UtcNow;

            var (newRefreshToken, rawRefreshToken) = AccessTokenFactory.CreateRefreshToken(existingToken.UserId, configuration);
            db.RefreshTokens.Add(newRefreshToken);

            try
            {
                // RefreshToken.RevokedAt is configured as a concurrency token (see
                // SheetMusicContext.OnModelCreating): if another request already revoked/rotated this
                // same token between our read and this save, the original value we loaded (null) no
                // longer matches what's in the database and this throws instead of silently overwriting
                // it - guaranteeing a token can never be redeemed more than once even under concurrent
                // replay.
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidRefreshTokenError();
            }

            var (accessToken, expiresAt) = AccessTokenFactory.CreateAccessToken(existingToken.UserId, configuration);

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
