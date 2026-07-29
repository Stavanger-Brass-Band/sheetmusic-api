using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Email;

/// <summary>
/// Logging no-op <see cref="IEmailSender"/> registered instead of <see cref="ResendEmailSender"/> when
/// <c>Resend:ApiKey</c> is not configured. Guards against an environment - such as a test environment
/// holding an anonymised copy of production data - ever sending real email through a live Resend key
/// that was configured by mistake: without a key, nothing can be sent regardless of what data is present.
/// </summary>
public class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetAsync(string toEmail, string displayName, string resetToken, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Password reset email to {Email} was not sent: no email provider is configured (Resend:ApiKey is absent).",
            toEmail);
        return Task.CompletedTask;
    }
}
