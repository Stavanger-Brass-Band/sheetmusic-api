using SheetMusic.Api.Email;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Infrastructure;

public class FakeEmailSender : IEmailSender
{
    public List<SentEmail> SentEmails { get; } = new();

    public Task SendPasswordResetAsync(string toEmail, string displayName, string resetToken, CancellationToken cancellationToken = default)
    {
        SentEmails.Add(new SentEmail(toEmail, displayName, resetToken));
        return Task.CompletedTask;
    }

    public void Clear() => SentEmails.Clear();
}

public record SentEmail(string ToEmail, string DisplayName, string ResetToken);
