using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Users.Authorization;
using SheetMusic.Api.Users.Errors;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Commands;

public sealed class RemoveProfilePicture(Guid userId, ClaimsPrincipal authenticatedUser) : IRequest
{
    public Guid UserId { get; } = userId;
    public ClaimsPrincipal AuthenticatedUser { get; } = authenticatedUser;

    public sealed class Handler(UserManager<ApplicationUser> userManager, IBlobClient blobClient, ILogger<Handler> logger) : IRequestHandler<RemoveProfilePicture>
    {
        public async Task Handle(RemoveProfilePicture request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.AuthenticatedUser.FindFirst(ClaimTypes.Name)?.Value, out var authenticatedUserId))
                throw new UnableToIdentifyUserError();

            var authenticatedUser = await userManager.FindByIdAsync(authenticatedUserId.ToString());
            var isAdmin = authenticatedUser is not null && await userManager.IsInRoleAsync(authenticatedUser, Roles.Admin);
            if (authenticatedUserId != request.UserId && !isAdmin)
                throw new ProfilePictureForbiddenError();

            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");
            if (user.ProfilePictureBlobName is null)
                throw new NotFoundError($"users/{request.UserId}/profile-picture", "Profile picture not found");

            var blobName = user.ProfilePictureBlobName;
            user.ProfilePictureBlobName = null;
            user.ProfilePictureVersion = null;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(error => error.Code == "ConcurrencyFailure"))
                    throw new ProfilePictureUpdateConflictError();

                throw new IdentityOperationError(result.Errors.Select(error => error.Description));
            }

            try
            {
                await blobClient.DeleteProfilePictureAsync(blobName, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete removed profile picture blob {BlobName}", blobName);
            }
        }
    }
}