using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Users.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Commands;

public class DeleteUser(Guid userId, bool hardDelete = false) : IRequest
{
    public Guid UserId { get; } = userId;
    public bool HardDelete { get; } = hardDelete;

    public class Handler(UserManager<ApplicationUser> userManager, SheetMusicContext db, IBlobClient blobClient, ILogger<Handler> logger) : IRequestHandler<DeleteUser>
    {
        public async Task Handle(DeleteUser request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");

            if (request.HardDelete)
                await DetachMusicianAsync(request.UserId, cancellationToken);

            var result = request.HardDelete
                ? await userManager.DeleteAsync(user)
                : await DeactivateAsync(user);

            if (!result.Succeeded)
                throw new IdentityOperationError(result.Errors.Select(e => e.Description));

            if (request.HardDelete && user.ProfilePictureBlobName is not null)
            {
                try
                {
                    await blobClient.DeleteProfilePictureAsync(user.ProfilePictureBlobName, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not delete profile picture blob {BlobName} for deleted user {UserId}", user.ProfilePictureBlobName, user.Id);
                }
            }

            async Task<IdentityResult> DeactivateAsync(ApplicationUser userToDeactivate)
            {
                userToDeactivate.Inactive = true;
                return await userManager.UpdateAsync(userToDeactivate);
            }

            async Task DetachMusicianAsync(Guid applicationUserId, CancellationToken ct)
            {
                var musician = await db.Musicians.SingleOrDefaultAsync(m => m.ApplicationUserId == applicationUserId, ct);
                if (musician is null)
                    return;

                musician.ApplicationUserId = null;
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
