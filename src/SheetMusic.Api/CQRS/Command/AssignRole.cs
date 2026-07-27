using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class AssignRole(Guid userId, string roleName) : IRequest
{
    public Guid UserId { get; } = userId;
    public string RoleName { get; } = roleName;

    public class Handler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager) : IRequestHandler<AssignRole>
    {
        public async Task Handle(AssignRole request, CancellationToken cancellationToken)
        {
            if (!await roleManager.RoleExistsAsync(request.RoleName))
                throw new RoleNotFoundError(request.RoleName);

            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");

            if (await userManager.IsInRoleAsync(user, request.RoleName))
                return;

            var result = await userManager.AddToRoleAsync(user, request.RoleName);

            if (!result.Succeeded)
                throw new IdentityOperationError(result.Errors.Select(e => e.Description));
        }
    }
}
