using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Queries;

public sealed class GetProfilePicture(Guid userId) : IRequest<GetProfilePicture.Result>
{
    public Guid UserId { get; } = userId;

    public sealed record Result(Stream Content, Guid Version);

    public sealed class Handler(UserManager<ApplicationUser> userManager, IBlobClient blobClient) : IRequestHandler<GetProfilePicture, Result>
    {
        public async Task<Result> Handle(GetProfilePicture request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");
            if (user.ProfilePictureBlobName is null || user.ProfilePictureVersion is null)
                throw new NotFoundError($"users/{request.UserId}/profile-picture", "Profile picture not found");

            try
            {
                var content = await blobClient.GetProfilePictureAsync(user.ProfilePictureBlobName, cancellationToken);
                return new Result(content, user.ProfilePictureVersion.Value);
            }
            catch (FileNotFoundException)
            {
                throw new NotFoundError($"users/{request.UserId}/profile-picture", "Profile picture not found");
            }
        }
    }
}