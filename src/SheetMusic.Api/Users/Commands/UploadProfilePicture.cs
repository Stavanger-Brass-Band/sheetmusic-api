using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Users.Authorization;
using SheetMusic.Api.Users.Errors;
using SheetMusic.Api.Users.RequestModels;
using SheetMusic.Api.Users.Services;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Commands;

public sealed class UploadProfilePicture(Guid userId, ClaimsPrincipal authenticatedUser, Stream content, ProfilePictureCropRequest crop) : IRequest<Guid>
{
    public Guid UserId { get; } = userId;
    public ClaimsPrincipal AuthenticatedUser { get; } = authenticatedUser;
    public Stream Content { get; } = content;
    public ProfilePictureCropRequest Crop { get; } = crop;

    public sealed class Handler(UserManager<ApplicationUser> userManager, IBlobClient blobClient, ProfilePictureProcessor processor, ILogger<Handler> logger) : IRequestHandler<UploadProfilePicture, Guid>
    {
        public async Task<Guid> Handle(UploadProfilePicture request, CancellationToken cancellationToken)
        {
            var authenticatedUserId = GetAuthenticatedUserId(request.AuthenticatedUser);
            var authenticatedUser = await userManager.FindByIdAsync(authenticatedUserId.ToString());
            var isAdmin = authenticatedUser is not null && await userManager.IsInRoleAsync(authenticatedUser, Roles.Admin);
            if (authenticatedUserId != request.UserId && !isAdmin)
                throw new ProfilePictureForbiddenError();

            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");
            await using var processedPicture = await processor.ProcessAsync(request.Content, request.Crop, cancellationToken);

            var newVersion = Guid.NewGuid();
            var newBlobName = $"profile-pictures/{user.Id}/{newVersion:N}.webp";
            await blobClient.AddProfilePictureAsync(newBlobName, processedPicture, cancellationToken);

            var previousBlobName = user.ProfilePictureBlobName;
            user.ProfilePictureBlobName = newBlobName;
            user.ProfilePictureVersion = newVersion;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                await DeleteBestEffortAsync(newBlobName, cancellationToken);
                if (result.Errors.Any(error => error.Code == "ConcurrencyFailure"))
                    throw new ProfilePictureUpdateConflictError();

                throw new IdentityOperationError(result.Errors.Select(error => error.Description));
            }

            if (previousBlobName is not null)
                await DeleteBestEffortAsync(previousBlobName, cancellationToken);

            return newVersion;
        }

        private static Guid GetAuthenticatedUserId(ClaimsPrincipal authenticatedUser)
        {
            if (!Guid.TryParse(authenticatedUser.FindFirst(ClaimTypes.Name)?.Value, out var userId))
                throw new UnableToIdentifyUserError();

            return userId;
        }

        private async Task DeleteBestEffortAsync(string blobName, CancellationToken cancellationToken)
        {
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
                logger.LogWarning(ex, "Could not delete superseded profile picture blob {BlobName}", blobName);
            }
        }
    }
}