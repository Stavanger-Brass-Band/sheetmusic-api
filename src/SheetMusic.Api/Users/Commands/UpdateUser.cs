using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Users.Authorization;
using SheetMusic.Api.Users.Errors;
using SheetMusic.Api.Users.RequestModels;
using SheetMusic.Api.Users.ViewModels;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Commands;

public class UpdateUser(Guid userId, ClaimsPrincipal authenticatedUser, UpdateUserRequest user) : IRequest
{
    public Guid UserId { get; } = userId;
    public ClaimsPrincipal AuthenticatedUser { get; } = authenticatedUser;
    public UpdateUserRequest User { get; } = user;

    public class Handler(UserManager<ApplicationUser> userManager, IOptions<IdentityOptions> identityOptions) : IRequestHandler<UpdateUser>
    {
        public async Task Handle(UpdateUser request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.AuthenticatedUser.FindFirst(ClaimTypes.Name)?.Value, out var authenticatedUserId))
                throw new UnableToIdentifyUserError();

            var currentUser = await userManager.FindByIdAsync(authenticatedUserId.ToString());
            var isAdmin = currentUser != null && await userManager.IsInRoleAsync(currentUser, Roles.Admin);
            var userToChange = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");

            if (authenticatedUserId != request.UserId && !isAdmin)
                throw new UserUpdateForbiddenError();

            var profileWasUpdated = false;

            if (!string.IsNullOrWhiteSpace(request.User.Name))
            {
                userToChange.DisplayName = request.User.Name;
                profileWasUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(request.User.Email))
            {
                userToChange.Email = request.User.Email;
                userToChange.UserName = request.User.Email;
                profileWasUpdated = true;
            }

            if (profileWasUpdated)
            {
                var result = await userManager.UpdateAsync(userToChange);

                if (!result.Succeeded)
                    throw new IdentityOperationError(result.Errors.Select(e => e.Description));
            }

            if (!string.IsNullOrWhiteSpace(request.User.Password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(userToChange);
                var result = await userManager.ResetPasswordAsync(userToChange, token, request.User.Password);

                if (!result.Succeeded)
                    throw PasswordRequirementsNotMetError.FromFailedResult(result, ApiPasswordRequirements.FromPasswordOptions(identityOptions.Value.Password));
            }
        }
    }
}