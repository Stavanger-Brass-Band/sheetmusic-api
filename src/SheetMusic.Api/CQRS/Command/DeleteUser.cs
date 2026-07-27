using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class DeleteUser(Guid userId, bool hardDelete = false) : IRequest
{
    public Guid UserId { get; } = userId;
    public bool HardDelete { get; } = hardDelete;

    public class Handler(UserManager<ApplicationUser> userManager) : IRequestHandler<DeleteUser>
    {
        public async Task Handle(DeleteUser request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");

            var result = request.HardDelete
                ? await userManager.DeleteAsync(user)
                : await DeactivateAsync(user);

            if (!result.Succeeded)
                throw new IdentityOperationError(result.Errors.Select(e => e.Description));

            async Task<IdentityResult> DeactivateAsync(ApplicationUser userToDeactivate)
            {
                userToDeactivate.Inactive = true;
                return await userManager.UpdateAsync(userToDeactivate);
            }
        }
    }
}
