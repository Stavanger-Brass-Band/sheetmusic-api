using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SheetMusic.Api.Users.RequestModels;

public sealed class ProfilePictureUploadRequest : ProfilePictureCropRequest
{
    public IFormFile File { get; set; } = null!;

    public sealed class Validator : AbstractValidator<ProfilePictureUploadRequest>
    {
        public Validator()
        {
            RuleFor(request => request.File).NotNull().WithMessage("A profile picture file is required");
        }
    }
}