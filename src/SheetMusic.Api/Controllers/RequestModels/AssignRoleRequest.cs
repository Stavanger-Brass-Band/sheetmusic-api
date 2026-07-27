using FluentValidation;

namespace SheetMusic.Api.Controllers.RequestModels;

public class AssignRoleRequest
{
    public string RoleName { get; set; } = null!;

    public class Validator : AbstractValidator<AssignRoleRequest>
    {
        public Validator()
        {
            RuleFor(r => r.RoleName).NotEmpty().WithMessage("A role name is required.");
        }
    }
}
