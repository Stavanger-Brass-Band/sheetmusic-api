using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class DeactivateUser(Guid userId) : IRequest
{
    public Guid UserId { get; } = userId;

    public class Handler(UserManager<ApplicationUser> userManager) : IRequestHandler<DeactivateUser>
    {
        public async Task Handle(DeactivateUser request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");

            user.Inactive = true;

            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new IdentityOperationError(result.Errors.Select(e => e.Description));
        }
    }
}
