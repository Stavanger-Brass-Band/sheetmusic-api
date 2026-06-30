using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Email;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string toEmail, string displayName, string resetToken, CancellationToken cancellationToken = default);
}
