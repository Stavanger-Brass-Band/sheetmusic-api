using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Users.ViewModels;

public class ApiUser
{
    public ApiUser(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        Id = user.Id;
        Name = user.DisplayName ?? user.UserName ?? null!;
        Email = user.Email ?? null!;
        Inactive = user.Inactive;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool Inactive { get; set; }
}
