namespace SheetMusic.Api.Configuration;

public static class ConfigKeys
{
    public const string JwtSigningKey = "Jwt:SigningKey";

    /// <summary>
    /// Optional prefix (e.g. "test", "prod") applied to Azure AI Search index names so multiple
    /// environments can share a single Free-tier search service without one environment's index
    /// rebuild deleting another's. Absent by default, which preserves the historical unprefixed
    /// index names for existing environments.
    /// </summary>
    public const string SearchIndexPrefix = "Search:IndexPrefix";
    public const string ResendApiKey = "Resend:ApiKey";
    public const string EmailFromAddress = "Email:FromAddress";
    public const string EmailFrontendBaseUrl = "Email:FrontendBaseUrl";
    public const string RateLimitingForgotPasswordPermitLimit = "RateLimiting:ForgotPassword:PermitLimit";
    public const string RateLimitingForgotPasswordWindowSeconds = "RateLimiting:ForgotPassword:WindowSeconds";
    public const string RateLimitingTokenPermitLimit = "RateLimiting:Token:PermitLimit";
    public const string RateLimitingTokenWindowSeconds = "RateLimiting:Token:WindowSeconds";

    /// <summary>
    /// Lifetime, in minutes, of issued JWT access tokens. Defaults to 15 when unset - see
    /// <see cref="Users.AccessTokenFactory"/>.
    /// </summary>
    public const string JwtAccessTokenExpiryMinutes = "Jwt:AccessTokenExpiryMinutes";

    /// <summary>
    /// Lifetime, in days, of issued refresh tokens. Defaults to 7 when unset - see
    /// <see cref="Users.AccessTokenFactory"/>.
    /// </summary>
    public const string JwtRefreshTokenExpiryDays = "Jwt:RefreshTokenExpiryDays";
}
