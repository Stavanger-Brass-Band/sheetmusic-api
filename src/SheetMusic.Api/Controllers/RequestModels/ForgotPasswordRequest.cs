using FluentValidation;

namespace SheetMusic.Api.Controllers.RequestModels;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;

    public class Validator : AbstractValidator<ForgotPasswordRequest>
    {
        public Validator()
        {
            RuleFor(r => r.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        }
    }
}
