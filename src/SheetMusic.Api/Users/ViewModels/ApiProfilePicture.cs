using System;

namespace SheetMusic.Api.Users.ViewModels;

public sealed class ApiProfilePicture(Guid version)
{
    public Guid Version { get; } = version;
}