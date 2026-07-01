namespace SheetMusic.Api.Configuration;

/// <summary>
/// Named rate limiting policies applied to anonymous, resource-sensitive endpoints.
/// </summary>
public static class RateLimitPolicies
{
    public const string ForgotPassword = "forgot-password";
    public const string Token = "token";
}
