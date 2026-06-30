using FluentValidation;

namespace SheetMusic.Api.Controllers.RequestModels;

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
            RuleFor(r => r.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        }
    }
}
