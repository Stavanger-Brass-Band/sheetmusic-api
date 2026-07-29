using FluentValidation;
using SheetMusic.Api.Users.Authorization;

namespace SheetMusic.Api.Users.RequestModels;

public class AssignRoleRequest
{
    /// <summary>
    /// The name of the role to assign. Must be one of <c>Admin</c>, <c>Noteansvarlig</c> or <c>Musikant</c>.
    /// </summary>
    public string RoleName { get; set; } = null!;

    public class Validator : AbstractValidator<AssignRoleRequest>
    {
        public Validator()
        {
            RuleFor(r => r.RoleName)
                .NotEmpty().WithMessage("A role name is required.")
                .Must(roleName => Roles.All.Contains(roleName))
                .WithMessage($"Role name must be one of: {string.Join(", ", Roles.All)}.");
        }
    }
}
