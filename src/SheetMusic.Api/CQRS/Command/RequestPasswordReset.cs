using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Email;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class RequestPasswordReset(string email) : IRequest
{
    public string Email { get; } = email;

    public class Handler(UserManager<ApplicationUser> userManager, IEmailSender emailSender) : IRequestHandler<RequestPasswordReset>
    {
        public async Task Handle(RequestPasswordReset request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null || user.Inactive)
                return;

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await emailSender.SendPasswordResetAsync(user.Email!, user.DisplayName ?? user.Email!, token, cancellationToken);
        }
    }
}
