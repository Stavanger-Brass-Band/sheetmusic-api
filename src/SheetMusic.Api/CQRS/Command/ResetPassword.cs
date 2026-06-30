using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class ResetPassword(string email, string token, string newPassword) : IRequest
{
    public string Email { get; } = email;
    public string Token { get; } = token;
    public string NewPassword { get; } = newPassword;

    public class Handler(UserManager<ApplicationUser> userManager) : IRequestHandler<ResetPassword>
    {
        public async Task Handle(ResetPassword request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null)
                throw new InvalidPasswordResetTokenError();

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
                throw new InvalidPasswordResetTokenError();
        }
    }
}
