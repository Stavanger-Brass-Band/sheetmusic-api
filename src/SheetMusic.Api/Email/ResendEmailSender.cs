using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using SheetMusic.Api.Configuration;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Email;

public class ResendEmailSender(IResend resend, IConfiguration configuration, ILogger<ResendEmailSender> logger) : IEmailSender
{
    public async Task SendPasswordResetAsync(string toEmail, string displayName, string resetToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var frontendBaseUrl = configuration[ConfigKeys.EmailFrontendBaseUrl]?.TrimEnd('/');
            var fromAddress = configuration[ConfigKeys.EmailFromAddress];

            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                logger.LogError("Cannot send password reset email: '{Key}' is not configured.", ConfigKeys.EmailFrontendBaseUrl);
                return;
            }

            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                logger.LogError("Cannot send password reset email: '{Key}' is not configured.", ConfigKeys.EmailFromAddress);
                return;
            }

            var encodedToken = WebUtility.UrlEncode(resetToken);
            var encodedEmail = WebUtility.UrlEncode(toEmail);
            var resetUrl = $"{frontendBaseUrl}/reset-password?token={encodedToken}&email={encodedEmail}";

            var message = new EmailMessage();
            message.From = fromAddress;
            message.To.Add(toEmail);
            message.Subject = "Reset your password";
            message.HtmlBody = BuildHtmlBody(displayName, resetUrl);
            message.TextBody = BuildTextBody(displayName, resetUrl);

            await resend.EmailSendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
        }
    }

    private static string BuildHtmlBody(string displayName, string resetUrl)
    {
        var encodedName = WebUtility.HtmlEncode(displayName);
        var encodedUrl = WebUtility.HtmlEncode(resetUrl);
        return $"""
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
              <h2>Password Reset Request</h2>
              <p>Hello {encodedName},</p>
              <p>We received a request to reset your password. Click the button below to reset it.
                 This link expires in <strong>1 hour</strong>.</p>
              <p>
                <a href="{encodedUrl}"
                   style="background-color:#4CAF50;color:white;padding:14px 20px;text-decoration:none;border-radius:4px;display:inline-block;">
                  Reset Password
                </a>
              </p>
              <p>Or copy and paste this link into your browser:</p>
              <p>{encodedUrl}</p>
              <p>If you did not request a password reset, you can safely ignore this email.</p>
            </body>
            </html>
            """;
    }

    private static string BuildTextBody(string displayName, string resetUrl) =>
        $"""
        Hello {displayName},

        We received a request to reset your password. Use the link below to reset it.
        This link expires in 1 hour.

        {resetUrl}

        If you did not request a password reset, you can safely ignore this email.
        """;
}
