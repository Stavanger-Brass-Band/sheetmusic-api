using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Users.Errors;
using SheetMusic.Api.Users.ViewModels;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Commands;

public class ResetPassword(string email, string token, string newPassword) : IRequest
{
    public string Email { get; } = email;
    public string Token { get; } = token;
    public string NewPassword { get; } = newPassword;

    public class Handler(UserManager<ApplicationUser> userManager, IOptions<IdentityOptions> identityOptions) : IRequestHandler<ResetPassword>
    {
        public async Task Handle(ResetPassword request, CancellationToken cancellationToken)
        {
            // Never distinguish an unknown email from any other failure here - doing so would let a
            // caller enumerate registered accounts.
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null)
                throw new InvalidPasswordResetTokenError();

            // UserManager.ResetPasswordAsync verifies the token via VerifyUserTokenAsync first and
            // returns an InvalidToken error immediately on failure, so password validators only run
            // once the token has been accepted. A password error below is therefore unreachable without
            // a valid, unexpired token.
            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
            {
                var passwordErrors = result.Errors.Where(PasswordRequirementsNotMetError.IsPasswordError).ToList();

                if (passwordErrors.Count > 0)
                    throw new PasswordRequirementsNotMetError(passwordErrors, ApiPasswordRequirements.FromPasswordOptions(identityOptions.Value.Password));

                throw new InvalidPasswordResetTokenError();
            }
        }
    }
}
