using Microsoft.Extensions.Configuration;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SheetMusic.Api.Users;

/// <summary>
/// Creates the two pieces of an OAuth2 password/refresh_token grant response: a short-lived, signed
/// JWT access token, and a longer-lived, opaque refresh token. Shared by <see cref="Commands.Login"/>
/// (initial issuance) and <see cref="Commands.RefreshAccessToken"/> (rotation), so both flows apply the
/// same expiry configuration and refresh token entropy.
/// </summary>
internal static class AccessTokenFactory
{
    /// <summary>
    /// Creates a signed JWT identifying <paramref name="userId"/> via a <see cref="ClaimTypes.Name"/> claim.
    /// Defaults to a 15 minute lifetime - refreshed via <see cref="Commands.RefreshAccessToken"/> using the
    /// paired refresh token - configurable via <see cref="ConfigKeys.JwtAccessTokenExpiryMinutes"/>.
    /// </summary>
    public static (string AccessToken, DateTime ExpiresAt) CreateAccessToken(Guid userId, IConfiguration configuration)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration[ConfigKeys.JwtSigningKey] ?? throw new MissingConfigurationException(ConfigKeys.JwtSigningKey));
        var expiryMinutes = GetPositiveConfiguredValue(configuration, ConfigKeys.JwtAccessTokenExpiryMinutes, 15);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, userId.ToString())]),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiresAt);
    }

    /// <summary>
    /// Builds a new, unpersisted <see cref="RefreshToken"/> for <paramref name="userId"/>, together with
    /// the raw (unhashed) token value to hand back to the caller. Only <see cref="HashToken"/>'s digest
    /// of the raw value is stored on the entity - never the raw value itself - so a leaked database
    /// (backup, log, dump) cannot be used to redeem outstanding refresh tokens. Defaults to a 7 day
    /// lifetime, configurable via <see cref="ConfigKeys.JwtRefreshTokenExpiryDays"/>. Callers are
    /// responsible for adding the entity to the <c>SheetMusicContext</c> and saving.
    /// </summary>
    public static (RefreshToken Entity, string RawToken) CreateRefreshToken(Guid userId, IConfiguration configuration)
    {
        var expiryDays = GetPositiveConfiguredValue(configuration, ConfigKeys.JwtRefreshTokenExpiryDays, 7);
        var now = DateTime.UtcNow;
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = HashToken(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(expiryDays)
        };

        return (entity, rawToken);
    }

    /// <summary>
    /// Computes the digest of a raw refresh token value as stored in <see cref="RefreshToken.Token"/>.
    /// Used both when persisting a newly issued token and when looking one up by its raw (presented)
    /// value.
    /// </summary>
    public static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static int GetPositiveConfiguredValue(IConfiguration configuration, string key, int defaultValue)
    {
        var configured = configuration.GetValue<int?>(key);

        return configured is > 0 ? configured.Value : defaultValue;
    }
}

