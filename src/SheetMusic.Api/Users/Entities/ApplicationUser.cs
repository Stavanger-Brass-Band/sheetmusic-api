using Microsoft.AspNetCore.Identity;

namespace SheetMusic.Api.Users.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public bool Inactive { get; set; } = true;
    public Musician? Musician { get; set; }
}
