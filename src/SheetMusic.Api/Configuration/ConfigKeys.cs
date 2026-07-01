namespace SheetMusic.Api.Configuration;

public static class ConfigKeys
{
    public const string Secret = "AppSettings:Secret";
    public const string SearchHost = "Search:Host";
    public const string SearchAdminKey = "Search:AdminKey";
    public const string ResendApiKey = "Resend:ApiKey";
    public const string EmailFromAddress = "Email:FromAddress";
    public const string EmailFrontendBaseUrl = "Email:FrontendBaseUrl";
    public const string RateLimitingForgotPasswordPermitLimit = "RateLimiting:ForgotPassword:PermitLimit";
    public const string RateLimitingForgotPasswordWindowSeconds = "RateLimiting:ForgotPassword:WindowSeconds";
    public const string RateLimitingTokenPermitLimit = "RateLimiting:Token:PermitLimit";
    public const string RateLimitingTokenWindowSeconds = "RateLimiting:Token:WindowSeconds";
}
