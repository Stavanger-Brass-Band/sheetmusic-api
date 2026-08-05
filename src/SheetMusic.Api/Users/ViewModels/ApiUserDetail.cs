using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Parts.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Users.ViewModels;

public class ApiUserDetail : ApiUser
{
    public ApiUserDetail(ApplicationUser user, IEnumerable<string> roles, IEnumerable<MusicPart> parts) : base(user)
    {
        Roles = roles.ToList();
        Parts = parts.Select(part => new ApiPart(part)).ToList();
    }

    public IReadOnlyList<string> Roles { get; }
    public IReadOnlyList<ApiPart> Parts { get; }
}
