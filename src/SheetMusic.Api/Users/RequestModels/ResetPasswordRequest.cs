using FluentValidation;

namespace SheetMusic.Api.Users.RequestModels;

public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;

    public class Validator : AbstractValidator<ResetPasswordRequest>
    {
        public Validator()
        {
            RuleFor(r => r.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(r => r.Token).NotEmpty().WithMessage("Reset token is required.");

            // Complexity rules are intentionally not duplicated here. Identity's PasswordValidator
            // (configured via IdentityOptions.Password in Program.cs) is the sole enforcement point for
            // all v2 password paths - see ResetPassword.Handler, which maps its failures to
            // PasswordRequirementsNotMetError. Re-adding rules here would run before the handler (this
            // validator runs via FluentValidation auto-validation) and shadow that error path entirely.
            RuleFor(r => r.NewPassword).NotEmpty().WithMessage("New password is required.");
        }
    }
}
