using System;

namespace SheetMusic.Api.Users.RequestModels;

public class UserRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}
